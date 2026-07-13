# Mot de passe oublié (password reset)

## Contexte

Un participant qui a activé son compte (`/mon-equipe`) mais oublie son mot de passe n'a aujourd'hui aucun moyen de le récupérer — seul l'admin peut renvoyer un lien d'activation, un par un, depuis `/teams`. Cette fonctionnalité ajoute un self-service "mot de passe oublié" pour les participants, en réutilisant au maximum le mécanisme d'activation déjà en place (`User.VerificationToken`/`VerificationTokenExpiresAt`, page `/activer-compte`, endpoint `POST /api/auth/activate`).

Le compte admin (compte unique, seedé hors ligne) est explicitement exclu de ce flow — il reste réinitialisable uniquement en base si besoin.

Le quota Mailgun de production est de 100 emails/jour, partagé avec l'inscription d'équipe et l'envoi bulk admin. Ce flow doit donc inclure une protection anti-abus qui ne peut pas, à elle seule, épuiser ce quota si quelqu'un de malveillant déclenche des demandes en rafale.

## 1. Backend — `POST /api/auth/forgot-password`

Nouvel endpoint public (pas de `[Authorize]`) sur `AuthController`, prenant un DTO `ForgotPasswordDto { Email }` (nouveau, `Models/User/ForgotPasswordDto.cs`, avec un validateur FluentValidation imposant un email non vide et de format valide — suit le pattern des autres DTO du dossier `Models/User/`).

Délègue à une nouvelle méthode `UserService.RequestPasswordResetAsync(string email, string? ipAddress)` :

1. **Vérifie la limite par IP** (voir section 2). Si dépassée → lève une nouvelle `RateLimitExceededException("Trop de tentatives depuis cette adresse. Réessayez plus tard.")`.
2. **Vérifie le plafond global journalier** (voir section 2). Si dépassé → lève la même exception avec le message `"Trop de demandes de réinitialisation aujourd'hui. Réessayez demain."`.
3. **Recherche le compte** : `_context.Users.Include(u => u.Team).ThenInclude(t => t.Players).FirstOrDefaultAsync(u => u.Username == email && u.Role == "User")`. Le filtre `Role == "User"` exclut explicitement le compte admin.
4. Si aucun compte trouvé → retourne normalement, sans rien faire de plus (aucun email envoyé).
5. Si un compte est trouvé mais qu'une demande a déjà été enregistrée pour cet email dans la fenêtre de cooldown (voir section 2) → retourne normalement, sans rien envoyer (silencieux — ce cas ne doit **jamais** être distingué du cas "email inconnu" côté réponse, sous peine de révéler qu'un compte existe pour cet email).
6. Sinon : régénère `VerificationToken` (nouveau token aléatoire, même génération que l'existant) et `VerificationTokenExpiresAt` (`UtcNow.AddDays(7)`, même durée que l'activation), sauvegarde (`SaveChangesAsync`), enregistre la demande dans les compteurs anti-abus (email + IP + global), puis envoie l'email de reset en best-effort (n'importe quelle erreur d'envoi ne doit jamais faire échouer la requête HTTP — même philosophie que l'envoi d'activation existant).

Le participant identifié pour la personnalisation de l'email ("Bonjour {Prénom}") est `user.Team.Players.OrderBy(p => p.Id).First()` — même règle de résolution du "participant 1" que le reste du code (`CreateOrRefreshAccountForTeamAsync`).

**Réponse du contrôleur :**
- Succès (email trouvé et envoyé, email introuvable, ou cooldown silencieux) → **200**, corps `{ message: "Si un compte existe pour cet email, un lien a été envoyé." }`, systématiquement identique dans les trois cas.
- `RateLimitExceededException` → **429**, corps `{ error: <message de l'exception> }`.

## 2. Anti-abus — `PasswordResetRateLimiter` (nouveau service singleton, en mémoire)

Nouveau fichier `Services/PasswordResetRateLimiter.cs`, enregistré via `builder.Services.AddSingleton<PasswordResetRateLimiter>()` dans `Program.cs` (singleton obligatoire — l'état doit survivre entre les requêtes HTTP au sein du même process ; contrairement aux autres services de ce projet qui sont scoped).

Trois compteurs indépendants, tous en mémoire (`ConcurrentDictionary`/compteurs simples protégés par verrou) — acceptable à cette échelle : les compteurs repartent à zéro si le backend redémarre, ce qui ne pose pas de problème pour ce cas d'usage (un seul conteneur backend tourne en prod).

- **Cooldown par email** : 15 minutes entre deux demandes réussies pour un même email. Vérifié/enregistré à l'étape 5 du flow ci-dessus — le rejet est **toujours silencieux** (jamais un statut différent), car révéler un rejet spécifique à cet email confirmerait son existence.
- **Limite par IP** : 5 demandes par heure et par adresse IP (fenêtre glissante). Contrairement au cooldown par email, le rejet ici renvoie **explicitement 429** — cette limite ne dépend pas de l'existence d'un compte, donc l'exposer ne fuit aucune information sur les emails enregistrés.
- **Plafond global** : 20 emails de reset envoyés par jour, fenêtre glissante de 24h (chaque envoi horodaté ; le compteur ne prend en compte que les envois des dernières 24h à l'instant de la requête). Rejet également explicite en 429, pour la même raison. Cette valeur laisse une marge confortable sous le quota Mailgun de 100/jour partagé avec l'inscription et l'envoi bulk admin.

Ces trois seuils (15 min / 5 par heure / 20 par jour) sont des constantes dans `PasswordResetRateLimiter`, ajustables sans changement d'architecture.

## 3. `MailService` — factoriser l'envoi, ajouter le template de reset

Le corps HTTP actuel de `SendActivationEmailAsync` (construction de la requête POST vers Mailgun, en-tête Basic Auth, form-encoding, gestion des erreurs/exceptions, retour `bool`) est extrait dans une méthode privée partagée, par exemple :

```csharp
private async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string text, string html)
```

`SendActivationEmailAsync` (existante, inchangée dans sa signature et son comportement pour les appelants) devient un appel à ce helper avec son sujet/texte actuel ("Activez votre compte...").

Nouvelle méthode publique `SendPasswordResetEmailAsync(string toEmail, string toName, string resetUrl)`, appelant le même helper avec un sujet et un texte dédiés à la réinitialisation (ex. sujet *"Réinitialisation de votre mot de passe - Sport Challenge Police 54"*, texte invitant à cliquer sur le lien pour définir un nouveau mot de passe, mention explicite *"Si vous n'êtes pas à l'origine de cette demande, ignorez cet email."*, et rappel de la durée de validité de 7 jours).

Le lien pointe vers `{FrontendBaseUrl}/activer-compte?token={token}` — exactement la même construction d'URL que `UserService.BuildActivationUrl`, réutilisée telle quelle (aucune nouvelle méthode de construction d'URL nécessaire).

## 4. Frontend

**`login.component.html`** : ajout d'un lien "Mot de passe oublié ?" sous le champ mot de passe (avant le bouton "Se connecter" ou juste après, au choix de l'implémentation — cohérent visuellement avec le reste du formulaire), routant vers une nouvelle page `/mot-de-passe-oublie`.

**Nouvelle page `ForgotPasswordComponent`** (`pages/forgot-password/`, standalone, route publique ajoutée à `app.routes.ts`) :
- Un seul champ email + bouton de soumission (pas de champ mot de passe ici — celui-ci reste sur `/activer-compte`).
- Appelle une nouvelle méthode `AuthService.forgotPassword(email: string)` → `POST /api/auth/forgot-password`.
- Sur réponse 200 : affiche le message générique renvoyé par l'API (ou un texte fixe côté frontend équivalent), quel que soit le cas réel côté serveur.
- Sur réponse 429 : affiche le message d'erreur spécifique renvoyé par l'API (distinct du cas générique — c'est le seul cas où le frontend distingue une erreur, puisque le 429 ne fuit aucune info par compte).
- Aucune redirection automatique après soumission : l'utilisateur doit aller consulter sa boîte mail. Le lien reçu par email l'amène directement sur `/activer-compte?token=...`, page déjà existante et fonctionnelle sans modification.

## Hors scope

- Réinitialisation du mot de passe admin via ce flow (reste manuel, en base).
- Changement de l'email de connexion (`User.Username`) via ce flow ou tout autre — toujours hors scope du projet, déjà documenté ailleurs.
- Persistance des compteurs anti-abus en base de données ou dans un cache partagé (Redis, etc.) — non nécessaire tant qu'un seul conteneur backend tourne en prod.
- Notification/alerte admin en cas de déclenchement du plafond global — le rejet silencieux (429 côté appelant) suffit pour ce volume.

## Vérification

1. `docker compose up --build`. Sur `/login`, cliquer "Mot de passe oublié ?" → arriver sur `/mot-de-passe-oublie`.
2. Soumettre l'email d'un participant déjà activé → message générique affiché, email reçu (ou log Mailgun si sandbox), lien menant à `/activer-compte?token=...` fonctionnel, nouveau mot de passe utilisable pour se reconnecter.
3. Soumettre un email inconnu → même message générique affiché, aucun email envoyé (vérifier les logs backend : pas d'appel Mailgun).
4. Soumettre deux fois de suite le même email valide (moins de 15 min d'écart) → même message générique les deux fois, mais un seul email réellement envoyé (vérifier logs/Mailgun).
5. Soumettre 6 fois en moins d'une heure depuis la même IP → la 6e requête renvoie 429 avec le message de limite IP.
6. `docker compose run --rm tests` → nouveaux tests d'intégration passent (happy path, email inconnu, cooldown, limite IP), suite complète toujours verte.

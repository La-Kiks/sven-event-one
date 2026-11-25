# Désabonnement des emails « prochaines éditions »

## Pourquoi

Le champ `Player.AcceptMails` (case facultative à l'inscription : « recevoir des emails pour les prochaines éditions ») est enregistré mais aujourd'hui personne ne peut le retirer soi-même : la politique de confidentialité dit « en nous contactant ». On prépare le terrain pour de futurs emails promotionnels (aucun n'existe encore) et on veut que chaque joueur, **participant 2 compris (sans compte)**, puisse retirer ce consentement en un clic. Le prochain email `/annonce` (nouvelle date de l'événement reporté) portera déjà le lien.

## Objectifs

- Lien de désabonnement personnel dans le pied de chaque email `/annonce` (y compris l'email de test).
- Page publique `/desabonnement` : confirmation par bouton, puis `AcceptMails = false`.
- Case « recevoir des emails pour les prochaines éditions » dans `mon-equipe`, **pour le participant 1 uniquement**.
- Politique de confidentialité mise à jour (retrait via le lien en bas des emails, ou en nous contactant).

## Non-objectifs

- Les annonces `/annonce` continuent de partir à **tous** les joueurs (message de service lié à l'inscription, cf. spec du 2026-11-13). Rien ne change dans les destinataires.
- Activation et mot de passe oublié : inchangés, pas de lien.
- Pas de bouton « me réabonner » sur la page publique (réabonnement : `mon-equipe` pour le participant 1, ou contact organisateur).
- Pas d'en-tête `List-Unsubscribe` (utile pour du volume marketing, à reconsidérer quand ces emails existeront).
- Pas d'envoi d'emails promotionnels.

## Anti-objectifs (échec même si ça « marche »)

- Désabonner quelqu'un parce que son antivirus/messagerie a ouvert le lien → le GET n'écrit rien, seul le bouton (POST) désabonne.
- Laisser croire au joueur qu'il ne recevra plus d'infos sur l'édition en cours → le libellé parle explicitement des **prochaines éditions**.
- Le participant 1 qui réabonne son coéquipier sans le vouloir en enregistrant `mon-equipe` → la valeur du participant 2 est conservée telle quelle côté serveur.
- Permettre de désabonner n'importe quelle adresse en devinant l'URL → signature obligatoire.
- Casser l'existant : pas de migration, inscription et envois actuels inchangés.

## Approches envisagées

- **A. Lien signé HMAC, sans stockage (retenue).** `/desabonnement?email=…&sig=…`, signature HMAC-SHA256 de l'email normalisé. Aucune migration. Les liens deviennent invalides si la clé change, acceptable.
- B. Jeton aléatoire par joueur en base : migration + remplissage des joueurs existants, révocable ; plus de changement pour peu de gain.
- C. Saisie de l'email + email de confirmation : envoi Mailgun en plus, anti-abus à prévoir ; trop lourd.

## Conception (A)

**Signature.** Email normalisé = `Trim().ToLowerInvariant()` (même règle que la déduplication des annonces). Clé HMAC dérivée de `Jwt:Key` avec un préfixe de contexte (ex. `SHA256("unsubscribe:" + Jwt:Key)`), pour ne pas ajouter de variable d'env tout en ne réutilisant pas la clé JWT telle quelle. Signature encodée base64url. Vérification en temps constant (`CryptographicOperations.FixedTimeEquals`). Un petit service dédié (ex. `UnsubscribeTokenService`) construit l'URL et vérifie.

**API.** `POST /api/mail-preferences/unsubscribe` public, corps `{ email, sig }` :
- signature invalide/absente → 400 ;
- valide → `AcceptMails = false` sur **tous** les `Player` ayant cet email (comparaison normalisée), 200. Idempotent ; 200 aussi si aucun joueur ne correspond (aucune fuite d'information, et le lien de l'email de test reste fonctionnel).

**Emails.** `BuildAnnouncementText/Html` reçoivent l'URL de désabonnement du destinataire et ajoutent sous le pied existant : « Vous ne souhaitez plus recevoir d'emails sur nos prochaines éditions ? Se désabonner : <lien> ». Lien construit sur `FrontendBaseUrl`, email URL-encodé, HTML-encodé dans la version HTML.

**Page `/desabonnement`** (publique, dans `app.routes.ts`) : lit `email` et `sig` dans l'URL, affiche l'adresse et un bouton « Confirmer le désabonnement ». États : paramètres manquants / lien invalide (400), envoi en cours, succès (« Vous ne recevrez plus d'emails sur nos prochaines éditions. Vous continuerez de recevoir les informations liées à votre inscription actuelle. »), erreur réseau. Style aligné sur `activer-compte`.

**`mon-equipe`.** Case visible pour le participant 1 seulement. Côté serveur, `UpdateMyTeamAsync` applique `AcceptMails` uniquement pour le participant 1 (`OrderBy(p => p.Id).First()`) et ignore la valeur envoyée pour le participant 2, ce qui protège aussi contre un client modifié.

**Politique de confidentialité.** « …que vous pouvez retirer à tout moment via le lien présent en bas de nos emails, ou en nous contactant. »

## Tests (backend, test-first)

- Signature : valide acceptée, email modifié / signature modifiée / absente refusés (400).
- Désabonnement : `AcceptMails` passe à false pour toutes les lignes de cet email, casse/espaces ignorés ; deuxième appel → 200.
- `BuildAnnouncementText/Html` contiennent le lien, email correctement encodé.
- `my-team` PUT : `acceptMails` du participant 2 ignoré, celui du participant 1 appliqué.

Frontend vérifié manuellement ; ajouter une section à `docs/manual-testing-guide.md`.

## Laissé au builder

Noms exacts des classes/fichiers, mise en page de la page `/desabonnement` dans le style existant, formulation finale des textes à la marge.

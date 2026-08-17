# Politique de confidentialité & consentement RGPD (inscription)

## Contexte

L'application collecte des données personnelles (nom, prénom, email, téléphone, catégorie, tenue, administration d'appartenance) pour les deux participants de chaque équipe, mais ne dispose d'aucune page légale (mentions légales / politique de confidentialité) et le consentement recueilli à l'inscription est mal formé : une seule case à cocher obligatoire ("Les informations de contact présentes dans ce formulaire ne seront utilisées que dans le cadre de cet événement ou événement similaire") pilote en réalité le champ `AcceptMails` des deux joueurs (`inscription-form.component.ts:197-208`), sans jamais mentionner l'envoi d'emails marketing. Résultat : 100% des inscrits sont techniquement opt-in sur `AcceptMails` dès qu'ils valident l'inscription, ce qui rend ce consentement invalide au sens RGPD (non spécifique, non librement donné puisque lié à l'exécution du contrat).

Cette pièce du chantier RGPD couvre : la page légale elle-même, son lien depuis le footer, et la correction du consentement à l'inscription. Sont explicitement hors scope (chantiers séparés déjà identifiés) : l'outillage d'export/suppression de données, la purge automatique après la période de rétention, et les DPA formels avec Mailgun/Yurplan/l'hébergeur.

**Éditeur / responsable de traitement** : Sven Barberat (organisateur individuel, pas de structure juridique formelle), contact `svenbarberat@orange.fr`, téléphone 06 48 73 50 15 (déjà public dans le footer et le formulaire). Hébergement dans l'Union Européenne. Durée de conservation : 1 an après l'événement, sauf ré-inscription.

## 1. Nouvelle page — `/politique-de-confidentialite`

Nouveau composant standalone `pages/legal/legal.component.{ts,html,scss}`, suivant le pattern des autres pages publiques (ex. `pages/not-found`) : contenu statique, pas d'appel backend, réutilise le composant UI `card` pour la cohérence visuelle avec le reste du site. Route publique ajoutée dans `app.routes.ts` : `{ path: "politique-de-confidentialite", component: LegalComponent }`.

Une seule page, avec deux sections ancrées (`#mentions-legales` et `#confidentialite`) plutôt que deux pages séparées — le site est petit et le contenu tient sur un écran.

**Section "Mentions légales"** :
- Éditeur : Sven Barberat, organisateur individuel — pas de société. Contact : email + téléphone ci-dessus.
- Hébergement : Union Européenne (pas de mention de transfert hors UE).

**Section "Politique de confidentialité"**, structurée en sous-parties :
- **Données collectées** : identité et coordonnées des deux participants (nom, prénom, email, téléphone), catégorie, taille de tenue, administration d'appartenance, nom d'équipe ; case "emails prochaines éditions" (`AcceptMails`).
- **Finalités et base légale** : exécution du contrat d'inscription à l'événement (nom, email, téléphone, catégorie, tenue, administration — nécessaires à l'organisation) ; consentement explicite pour l'envoi d'emails sur les futures éditions (`AcceptMails`, optionnel).
- **Destinataires** : Mailgun (envoi des emails transactionnels d'activation de compte) ; Yurplan (billetterie partenaire — paiement effectué directement sur leur site, régi par leur propre politique de confidentialité ; ce site ne leur transmet aucune donnée automatiquement, le lien de paiement est statique).
- **Durée de conservation** : 1 an après l'événement, puis suppression sauf ré-inscription à une édition suivante.
- **Droits des personnes** : accès, rectification, effacement, portabilité, opposition — exercables par email à `svenbarberat@orange.fr`. Mention du droit de réclamation auprès de la CNIL (cnil.fr).

## 2. Lien depuis le footer

Dans `landing.component.html:157-160`, ajout d'un lien vers `/politique-de-confidentialite` à côté des informations de contact déjà présentes dans le `<footer>`.

## 3. Correction du consentement à l'inscription

Dans `inscription-form.component.html`, step 3 (autour de la ligne 332), remplacement de l'unique case "subscribe" par deux cases distinctes :

- **Case obligatoire** (garde `formControlName="subscribe"`, `Validators.requiredTrue`, comportement de validation inchangé) : *"J'ai pris connaissance de la [politique de confidentialité](/politique-de-confidentialite)"* — lien cliquable vers la nouvelle page (nouvel onglet). Ne pilote plus `acceptMails`.
- **Nouvelle case optionnelle**, décochée par défaut, sans validateur `requiredTrue` : *"Je souhaite recevoir des emails pour les prochaines éditions"* — `formControlName="acceptFutureEmails"` (nouveau `FormControl(false)` dans le groupe `step3`).

Dans `inscription-form.component.ts`, remplacement de `acceptMails: !!step3.subscribe` (lignes 198 et 208) par `acceptMails: !!step3.acceptFutureEmails` pour les deux joueurs — le champ reste partagé entre les deux participants (une seule personne remplit le formulaire pour l'équipe), mais reflète désormais un choix réellement optionnel et explicite plutôt qu'une conséquence automatique de la validation du formulaire.

Aucun changement backend : `AcceptMails` est déjà un booléen simple dans `CreatePlayerDto`/`Player`, seule la valeur envoyée par le frontend change de source.

## Hors scope

- Export ou suppression en self-service des données d'un participant.
- Purge automatique des données après la période de rétention (1 an) — actuellement un rappel manuel/futur chantier.
- DPA formels avec Mailgun, Yurplan, l'hébergeur.
- Consentement individualisé du participant 2 (actuellement recueilli par le participant 1 qui remplit le formulaire pour l'équipe) — limitation pragmatique documentée dans la politique de confidentialité elle-même plutôt que résolue techniquement.
- Deux pages séparées "mentions légales" / "politique de confidentialité" — une seule page avec ancres suffit à ce stade.

## Vérification

1. `docker compose up --build`. Depuis `/`, cliquer le nouveau lien du footer → arriver sur `/politique-de-confidentialite`, contenu lisible, mentions légales + politique de confidentialité présentes avec les bonnes coordonnées.
2. Sur `/inscription`, arriver à l'étape 3 : la case "politique de confidentialité" est obligatoire (le bouton "Continuer" refuse tant qu'elle n'est pas cochée), son lien ouvre bien `/politique-de-confidentialite`. La case "emails prochaines éditions" est décochée par défaut et n'empêche pas de continuer si elle reste décochée.
3. Compléter une inscription sans cocher "emails prochaines éditions" → vérifier en base (table `Players`) que `AcceptMails` vaut `false` pour les deux joueurs.
4. Compléter une inscription en cochant "emails prochaines éditions" → vérifier que `AcceptMails` vaut `true` pour les deux joueurs.

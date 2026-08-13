# Handoff : refonte design — Hyrox Police 54

Repo cible : `La-Kiks/sven-event-one`, branche `main`, dossier `ui/` (Angular 19, standalone components, SCSS).

## Overview

Refonte visuelle des 4 écrans publics/privés du site d'inscription Hyrox Police 54 :
landing, formulaire d'inscription (4 étapes), page participant `/mon-equipe`, panneau admin
`/teams` + `/players`. L'identité jaune `#ffed00` / noir `#000` est conservée, ainsi que les
polices déjà installées (Lemon Milk pour les titres, Cabin pour le texte). Aucune modification
de logique métier, de routes, de formulaires réactifs ou d'API n'est demandée — uniquement le
markup et les styles.

## About the design files

Les fichiers de `designs/` sont des **références de design écrites en HTML** : des prototypes
qui montrent l'apparence et le comportement voulus. **Ce n'est pas du code à copier tel quel.**
Le travail consiste à **recréer ces écrans dans le codebase Angular existant**, avec ses
patterns actuels : composants standalone, templates `.component.html`, SCSS par composant,
variables de `ui/src/styles/_variables.scss`, `ReactiveFormsModule`, `RouterLink`, services HTTP.

Les valeurs sont écrites en styles inline dans les prototypes ; côté Angular elles doivent
repartir dans les fichiers `.scss` des composants, en réutilisant les variables SCSS
(`$main-color`, `$background-color`, `$font-title`, `$font-body`, échelle `$text-*`).

## Fidelity

**High-fidelity.** Couleurs, typographies, espacements et interactions sont définitifs.
À reproduire fidèlement, en gardant les composants Angular existants (`app-button`, `app-card`,
`app-modal`) là où ils s'appliquent, quitte à en ajuster les styles.

---

## Design tokens

| Token | Valeur | Usage |
|---|---|---|
| Jaune principal | `#ffed00` | accents, CTA, chiffres clés, bordures actives |
| Noir fond | `#000000` | fond de page |
| Noir surface | `#0a0a0a` | cartes, tuiles, panneaux |
| Noir champ | `#141414` | inputs et selects |
| Noir champ verrouillé | `#1c1c1c` | input readonly (email de login) |
| Bordure | `rgba(255,255,255,0.1)` | contours de cartes et tableaux |
| Bordure champ | `rgba(255,255,255,0.14)` | contour d'input |
| Texte | `#ffffff` | texte principal |
| Texte secondaire | `rgba(255,255,255,0.55)` | paragraphes |
| Texte tertiaire | `rgba(255,255,255,0.4)` | labels, méta |
| Succès | `#4ade80` sur `rgba(34,197,94,0.15)` | payé, activé, bénévole |
| Erreur | `#ff6b6b` sur `rgba(255,107,107,0.1)` | non payé |
| Danger | `#f87171` / bordure `rgba(220,38,38,0.35)` | suppression |
| Catégorie homme | `#93c5fd` sur `rgba(59,130,246,0.12)` | badge |
| Catégorie femme | `#f9a8d4` sur `rgba(236,72,153,0.12)` | badge |
| Catégorie mixte | `#d8b4fe` sur `rgba(168,85,247,0.12)` | badge |

**Rayons** : `2px` partout (au lieu des `0.5rem` actuels) — le design est volontairement
angulaire ; seul le logo-mark garde `3px`.

**Typographie**
- Titres : `Lemon` (Lemon Milk), majuscules, `letter-spacing: -0.01em` sur les gros titres.
- Corps : `Cabin`.
- H1 landing : `clamp(2.8rem, 9vw, 7rem)`, `line-height: 0.92`.
- H1 de page (form/admin) : `clamp(1.8rem, 4vw, 2.6rem)`.
- H2 de section : `clamp(1.9rem, 4.5vw, 3.2rem)` (landing), `1.5rem` (form).
- Sur-titres / labels : `0.72–0.78rem`, `letter-spacing: 0.14–0.2em`, majuscules,
  `rgba(255,255,255,0.4–0.5)`.
- Corps : `1rem`, `line-height: 1.55`.

**Espacements** : sections `clamp(3rem, 7vw, 5.5rem)` vertical, gouttières de page
`clamp(1rem, 4vw, 3rem)`, largeur max de contenu `1240px` (landing), `1180px` (form/admin),
`980px` (mon équipe).

**Grilles séparées par filets** : beaucoup de blocs (chiffres clés, tuiles de statut, grille
partenaires) utilisent `display:grid; gap:1px; background:rgba(255,255,255,0.1)` avec des
enfants `background:#0a0a0a` — les filets sont donc le fond qui transparaît, pas des bordures.

---

## Écran 1 — Landing (`ui/src/app/pages/landing/`)

Fichier de référence : `designs/Landing refonte.dc.html`
(l'état actuel recréé à l'identique est dans `designs/Landing actuel.dc.html`, pour comparaison).

**Header** — `position: sticky; top: 0`, fond `rgba(0,0,0,0.82)` + `backdrop-filter: blur(10px)`,
bordure basse `1px rgba(255,255,255,0.08)`, `flex-wrap: wrap` (indispensable en mobile).
Contenu : carré jaune 34×34 « 54 » (Lemon, `border-radius:3px`), wordmark « HYROX POLICE 54 »
(Lemon `0.8rem`, `letter-spacing:0.14em`), liens d'ancrage Format / Partenaires / Infos
(masqués par défaut, à afficher ≥ 900px), bouton fantôme « Se connecter » (`/login`) et CTA
jaune « S'inscrire — 60 € » (`/inscription`).

**Hero** — `min-height: min(88vh, 860px)`, contenu aligné en bas.
Fond : `linear-gradient(to bottom, rgba(0,0,0,0.55) 0%, rgba(0,0,0,0.35) 35%, rgba(0,0,0,0.92) 88%, #000 100%)`
au-dessus de `images/gym-competitors-large.jpg` (`background-position: center 30%`).
- Eyebrow : trait jaune 34×3px + « SEPTEMBRE 2026 · LAXOU (54) » en jaune, `0.8rem`, `letter-spacing:0.24em`.
- H1 : « HYROX » / « POLICE 54 » (2e ligne en jaune).
- Paragraphe : max `44ch`, `rgba(255,255,255,0.72)`.
- CTA : bouton jaune plein « S'INSCRIRE EN DUO — 60 € » + bouton bordé « VOIR LE FORMAT » (ancre `#format`).
- Bandeau de chiffres clés : grille `repeat(auto-fit, minmax(150px, 1fr))`, filets 1px,
  valeurs en Lemon jaune `clamp(1.5rem,3vw,2.2rem)`, libellés `0.75rem`/`0.16em` :
  **8 Ateliers · 8 km De course · 2 Équipiers · 60 € Par duo**.

**Vidéo** — titre « L'ÉVÉNEMENT EN VIDÉO » + sous-titre « Le format, les épreuves et le déroulé
de l'inscription, expliqués en quelques minutes. » Puis l'iframe YouTube existante
(`iu4gl2vs--s`), `aspect-ratio: 16/9`, avec un cadre jaune 2px décalé derrière
(`position:absolute; inset:14px -14px -14px 14px`).

**Bandeau jaune** — pleine largeur, fond `#ffed00`, texte noir Lemon `letter-spacing:0.18em` :
« 1 KM RUN · AVANT CHAQUE ÉPREUVE · … » (`white-space: nowrap; overflow: hidden`).

**Format (`#format`)** — titre + méta « 8 ateliers · 8 km ». Grille
`repeat(auto-fit, minmax(260px,1fr))`, `gap: 1.2rem`. Chaque carte : fond `#111`, bordure
`rgba(255,255,255,0.08)`, image `aspect-ratio: 4/3`, `object-fit: cover`,
`filter: grayscale(100%) contrast(1.1) brightness(0.85)` ; pastille numéro jaune collée au coin
haut-gauche (`top:0; left:0`, Lemon `1.05rem`, padding `0.45rem 0.7rem`) ; sous l'image, nom de
l'atelier en Lemon majuscules puis la métrique en jaune `0.8rem`/`0.16em`.
Ordre et contenus (inchangés vs. `landing.component.html`) : 01 Ski Erg 1000 m · 02 Sled Push 50 m ·
03 Sled Pull 50 m · 04 Burpees Broad Jump 80 m · 05 Rowing 1000 m · 06 Farmers Carry 200 m ·
07 Fentes 100 m · 08 Wall Balls 100 x.

**Partenaires (`#partenaires`)** — grille `repeat(auto-fit, minmax(140px,1fr))`, filets 1px,
tuiles carrées `aspect-ratio: 1/1`, fond `#0a0a0a`, padding `1rem`, logo en
`object-fit: contain` (et non `cover` comme aujourd'hui : les logos ne doivent plus être rognés).
Ordre : Police Nationale, Orphéopolis, FSPN, Well & Fit, CrossFit Laxou, Fitness Park, Fitnrack,
Intersport, MGP, BFM, Cops13, RW.

**Infos pratiques (`#infos`)** — deux colonnes `repeat(auto-fit, minmax(300px,1fr))` :
à gauche l'iframe Google Maps existante (**sans filtre CSS** — couleurs Google d'origine),
`min-height: 340px` ; à droite une pile de 3 tuiles séparées par des filets : Lieu
(Well & Fit, 113 Bd Emile Zola, 54520 Laxou), Contact organisateur (Sven Barberat,
06 48 73 50 15, svenbarberat@orange.fr en jaune), Inscription (rappel des 60 € / activation par email).

**Bandeau CTA final (`#inscription`)** — pleine largeur jaune, texte noir :
« TROUVE TON BINÔME. / INSCRIS TON ÉQUIPE. » + « Places limitées · 60 € par duo · Septembre 2026 »
et un bouton noir/jaune « S'INSCRIRE EN DUO » vers `/inscription`.

**Footer** — filet haut, deux lignes `0.8rem` `rgba(255,255,255,0.4)` :
mention Orphéopolis à gauche, lieu + téléphone à droite.

**État complet (`isRegistrationFull`)** — la logique existante est conservée : quand le compteur
d'équipes renvoie `isFull`, les CTA passent en variante désactivée/contact (voir
`ButtonComponent`) ; le bandeau final devient un renvoi vers `#infos`.

---

## Écran 2 — Inscription (`ui/src/app/components/ui/inscription-form/`)

Fichier de référence : `designs/Inscription refonte.dc.html`

Même `FormGroup` en 4 étapes (`step1`, `step2`, `step3` + récapitulatif), même
`sessionStorage` de brouillon, même navigation par `?step=`. Seule la présentation change.

**Layout** — header simple (logo + « INSCRIPTION DUO · 60 € »), puis deux colonnes
`repeat(auto-fit, minmax(280px,1fr))`, max `1180px`.
- **Colonne gauche (rail d'étapes)** : titre « INSCRIPTION / EN DUO » (2e ligne jaune),
  phrase d'accroche, puis la liste des 4 étapes (01 Participant 1, 02 Participant 2, 03 Équipe,
  04 Récapitulatif) en boutons cliquables : fond `#0a0a0a`, filets 1px, `border-left: 2px solid`
  jaune sur l'étape active, numéro en Lemon jaune si étape active ou déjà faite, gris sinon.
  Bas de colonne : téléphone + email de l'organisateur en `0.8rem`.
  **`position: sticky; top: 1.5rem` uniquement à partir de 860px de large** ; en dessous,
  `position: static` (sinon le formulaire défile sous le rail).
- **Colonne droite (carte de l'étape)** : fond `#0a0a0a`, bordure `rgba(255,255,255,0.1)`,
  padding `clamp(1.25rem,3vw,2.25rem)`. En tête : « ÉTAPE n / 4 » en jaune + barre de
  progression 2px (`width: n*25%`, remplissage jaune).

**Champs** — `display:flex; flex-direction:column; gap:0.45rem`. Le libellé et son astérisque
jaune doivent être **sur la même ligne** (les envelopper dans un `span` en `display:flex`).
Input : fond `#141414`, bordure `rgba(255,255,255,0.14)`, rayon `2px`, `padding: 0.8rem 0.9rem`,
texte blanc `1rem`. Focus : bordure jaune. Erreur : bordure `#ff6b6b` + message `0.78rem` `#ff6b6b`
(mêmes messages que `requiredMessages`).
Grille des champs : `repeat(auto-fit, minmax(220px,1fr))`, `gap: 1rem`.

**Radios → chips.** Les `input[type=radio]` restent dans le DOM pour l'accessibilité et pour
`ReactiveFormsModule`, mais sont visuellement remplacés par des « chips » (label stylé) :
inactif = fond transparent, bordure `rgba(255,255,255,0.18)`, texte `rgba(255,255,255,0.75)` ;
actif = fond `#ffed00`, texte noir, `font-weight: 700`. Padding `0.7rem 1.1rem`, rayon `2px`,
disposés en `flex-wrap` avec `gap: 0.6rem`. Concerne Catégorie (Homme/Femme/Mixte),
Tenue d'intervention (3 réponses), Version (Courte/Longue).

**Cases à cocher** (bénévole, consentement) — ligne complète cliquable : bordure `1px`
(`rgba(255,255,255,0.14)`, jaune 50 % si cochée), padding `0.9rem 1rem`, carré 18×18 rempli en
jaune quand coché, texte `0.92rem` `line-height:1.45`.

**Étape 4 — récapitulatif** — une section par bloc (Participant 1, Participant 2, Équipe) :
bordure `rgba(255,255,255,0.12)`, en-tête `rgba(255,255,255,0.02)` avec le titre en Lemon
`0.95rem` et un bouton « MODIFIER » bordé jaune qui renvoie vers l'étape correspondante.
Contenu en `<dl>` `grid-template-columns: minmax(110px,auto) 1fr`, `dt` en label majuscule
gris, `dd` en `rgba(255,255,255,0.85)`. Sous les sections, encart d'avertissement :
`border-left: 2px solid #ffed00`, fond `rgba(255,237,0,0.06)` — texte sur la redirection
billetterie + email d'activation.

**Barre d'actions** — « Retour » (bordé, seulement à partir de l'étape 2) et bouton jaune
`flex: 1` : « Continuer », puis « Confirmer et payer 60 € » à l'étape 4. Conserver l'état
`isSubmitting` avec le spinner existant et `app-modal` pour succès/erreur.

---

## Écran 3 — Mon équipe (`ui/src/app/pages/my-team/`)

Fichier de référence : `designs/Mon equipe refonte.dc.html`

**Header** — logo + email de l'utilisateur connecté + bouton « Se déconnecter » bordé.

**Titre** — sur-titre « MON ÉQUIPE » puis nom de l'équipe en Lemon
`clamp(2rem,5vw,3.2rem)`; à droite, deux pastilles bordées : version et administration.

**Bandeau de statut** — 3 tuiles `repeat(auto-fit, minmax(240px,1fr))` séparées par des filets :
1. **Paiement** — `border-left: 2px solid` vert si payé / rouge sinon ; valeur en Lemon
   (`Payé` `#4ade80` / `Non payé` `#ff6b6b`) ; texte d'aide : payé → « 60 € reçus pour ton
   équipe. Rien d'autre à faire. » ; non payé → « Le paiement peut mettre 48 h à s'actualiser.
   Passé ce délai, contacte un organisateur. » (garder les liens tel/mail existants).
2. **Compte** — Activé / En attente.
3. **Événement** — date + lieu.

**Sections d'édition** — une carte par bloc (Équipe, Participant 1, Participant 2), bordure
`rgba(255,255,255,0.1)`, en-tête `rgba(255,255,255,0.02)` avec titre Lemon `0.95rem`
majuscules (numéro `01`/`02` en jaune pour les participants). Champs et chips identiques à
l'écran d'inscription. L'email du participant 1 reste `readonly` : fond `#1c1c1c`, texte
`rgba(255,255,255,0.45)`, `cursor: not-allowed`, avec la phrase d'explication en dessous.

**Barre d'enregistrement** — `position: sticky; bottom: 0`, dégradé
`linear-gradient(to top,#000 60%,transparent)`, bouton jaune « ENREGISTRER LES MODIFICATIONS »
(désactivé si `form.invalid || isSaving`). Les bannières de succès/erreur existantes gardent
leurs couleurs vert/rouge.

---

## Écran 4 — Admin (`ui/src/app/pages/teams/` et `ui/src/app/pages/players/`)

Fichier de référence : `designs/Admin refonte.dc.html`
(le prototype réunit les deux routes derrière un onglet ; dans l'app ce sont deux pages
et l'onglet correspond à la nav existante `/teams` ↔ `/players`).

**Interface entièrement en français** (aujourd'hui mélangée EN/FR) :
Teams → Équipes, Players → Joueurs, Sign out → Déconnexion, Delete → Supprimer,
Paid/Unpaid → Payé/Non payé, Volunteer → Bénévole, Search… → « Rechercher un nom, une équipe,
un email… », Export CSV inchangé.

**Header** — logo « 54 » + « ADMINISTRATION » en Lemon `0.78rem`, onglets Équipes / Joueurs
(actif : fond `rgba(255,255,255,0.06)`, bordure `rgba(255,255,255,0.15)`, texte blanc),
utilisateur + bouton Déconnexion. `flex-wrap: wrap` obligatoire.

**Page Équipes**
- Titre + compteur « n équipes · n joueurs » + bouton bordé jaune « Envoyer les emails
  d'activation (n en attente) » aligné à droite (comportement existant conservé).
- **Bandeau KPI** (nouveau) : 4 tuiles `repeat(auto-fit, minmax(160px,1fr))` séparées par
  filets — Équipes, Joueurs, Payées, En attente ; valeur en Lemon jaune `1.6rem`.
- **Tableau** : conteneur bordé avec `overflow-x: auto` et un contenu `min-width: 720px`
  (indispensable sur mobile). Colonnes `2fr 1fr 1fr 1.2fr 1fr` : Équipe (index `01` en Lemon
  gris + nom), Catégorie (badge coloré), Version, Administration, Paiement (badge).
  Ligne = élément cliquable, `border-bottom: 1px rgba(255,255,255,0.05)` ; ligne sélectionnée :
  fond `rgba(255,255,255,0.04)` + `border-left: 2px solid #ffed00`.
- **Panneau de détail** : `position: fixed`, droite, `width: 460px; max-width: 95vw`, fond
  `#0a0a0a`, `transform: translateX(100%→0)` en `0.3s cubic-bezier(0.4,0,0.2,1)`, scrim
  `rgba(0,0,0,0.55)`. En-tête : bouton ✕, nom de l'équipe en Lemon, bouton « Supprimer » rouge.
  Corps : liste d'infos label/valeur (Catégorie, Version, Administration, Joueurs, Compte),
  puis les deux actions (« Marquer comme payé/non payé » — jaune plein quand l'action rend payé,
  bordé sinon — et « Renvoyer l'email d'activation »), puis les fiches joueurs (nom, catégorie,
  email, téléphone, tags Bénévole / Tenue).
  Garder les confirmations existantes (suppression en rouge, bascule de paiement).

**Page Joueurs**
- Titre + compteur, barre d'outils : champ de recherche (`flex: 1 1 240px`, fond `#141414`),
  chips de filtre Tous / Bénévoles / Non bénévoles (actif = jaune plein), select de catégorie,
  bouton « Export CSV (n) » aligné à droite.
- Tableau : même conteneur `overflow-x: auto`, contenu `min-width: 900px`, colonnes
  `1.4fr 1.2fr 0.8fr 1.6fr 1fr 0.8fr` — Nom, Équipe (pastille cliquable vers `/teams?teamId=`),
  Catégorie, Email, Téléphone, Bénévole (badge vert Oui / neutre Non).
- Conserver le tri par colonne, `aria-sort`, l'export CSV filtré et les états
  chargement / erreur / vide (le shimmer existant reste valable).

---

## Interactions & comportements

- **Ancres landing** : `#format`, `#partenaires`, `#infos`, `#inscription` (scroll fluide déjà en place).
- **Transitions** : panneau admin `0.3s cubic-bezier(0.4,0,0.2,1)` ; le reste en `0.15s ease`
  (hover de boutons, chips, lignes de tableau). Pas d'animation d'apparition au scroll.
- **Hover** : CTA jaune → `filter: brightness(1.1)` ; boutons bordés → bordure jaune + texte
  blanc ; ligne de tableau → fond `rgba(255,255,255,0.04)`.
- **Focus visible** : `outline: 2px solid #ffed00; outline-offset: 2px` — à conserver partout
  (déjà présent dans le SCSS actuel).
- **Cibles tactiles** : 44px minimum ; c'est la raison des chips à la place des radios natifs.

## Responsive

Un seul point de rupture réel : **860px**.
- Sous 860px : rail d'étapes non sticky, colonnes empilées, tableaux admin en scroll horizontal,
  headers en `flex-wrap`.
- Les grilles utilisent `auto-fit` + `minmax()` et se réorganisent seules : stations 260px,
  partenaires 140px, champs 220px, tuiles 240px.
- Sous 900px, masquer les liens d'ancrage du header landing (le CTA reste visible).

## Assets

Aucun nouvel asset. Tout provient déjà du repo :
- `ui/public/images/gym-competitors-large.jpg` (hero)
- `ui/public/images/{ski-erg,sled-push,sled-pull,burpees,rameur,farmeur,fente,wall-ball}.webp`
- `ui/public/images/sponso-*.{JPG,JPEG,PNG}`
- `ui/public/fonts/Lemon/LEMONMILK-Regular.otf`, `ui/public/fonts/Cabin/Cabin-*.ttf`
- iframes YouTube et Google Maps : URLs inchangées.

## Files

Dans `designs/` :
- `Landing refonte.dc.html` — landing redessinée
- `Landing actuel.dc.html` — recréation fidèle de l'existant (pour comparer avant/après)
- `Inscription refonte.dc.html` — formulaire 4 étapes
- `Mon equipe refonte.dc.html` — page participant (`isPaid` bascule l'état payé/non payé)
- `Admin refonte.dc.html` — équipes + joueurs + panneau de détail
- `Apercu mobile.dc.html` — les 4 écrans en cadres 390 × 844
- `support.js` — runtime nécessaire pour ouvrir les fichiers ci-dessus dans un navigateur

Les fichiers `.dc.html` s'ouvrent directement dans un navigateur (garder `support.js` à côté).

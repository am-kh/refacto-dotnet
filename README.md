### Consignes .NET: 
* Ignorez les migrations BDD
* Ne pas modifier les classes qui ont un commentaire: `// WARN: Should not be changed during the exercise
`
* Pour lancer les tests: `dotnet test`

---

## Refactoring

### Démarche
1. Migration vers .NET 10 (seul SDK disponible).
2. **Tests de caractérisation** écrits *avant* toute modification : chaque cas métier est figé au niveau HTTP (stock relu en base + notifications vérifiées). Leur efficacité a été contrôlée par test de mutation.
3. Refactoring par petites étapes, les tests de caractérisation restant inchangés et verts.
4. Tests unitaires ciblés sur les cas limites de dates, avec un instant fixe.

### Architecture
```
Refacto.DotNet.Api ──► Refacto.DotNet.Application ──► Refacto.DotNet.Domain
        └────────────► Refacto.DotNet.Infrastructure ──► Application
```
| Projet | Rôle |
|---|---|
| **Domain** | Entités `Order`, `Product` (règles de stock, saison, expiration), `ProductTypes`. Aucune dépendance. |
| **Application** | Cas d'usage `IOrderService`, un `IProductOrderHandler` par type de produit, ports `IOrderRepository` et `INotificationService`. |
| **Infrastructure** | EF Core (mapping Fluent), `OrderRepository`, `NotificationService`, horloge système. |
| **Api** | `OrdersController` : HTTP uniquement (200 / 404). |

### Choix de conception
- **Strategy par type de produit** : ajouter un type = ajouter un handler et l'enregistrer, sans modifier l'existant (Open/Closed).
- **`TimeProvider`** à la place de `DateTime.Now` : l'instant est lu une seule fois par commande et transmis aux handlers, qui restent purs et testables.
- **Une seule sauvegarde par commande** (tout ou rien) au lieu d'un `SaveChanges` par produit.
- **Mapping Fluent** dans Infrastructure : le domaine ne porte aucune préoccupation de persistance. Les noms de tables/colonnes, ignorés auparavant par le provider InMemory, sont désormais appliqués.
- **404** pour une commande introuvable (auparavant `NullReferenceException` → 500).
- Classes `internal sealed` par défaut ; seuls les contrats entre couches sont publics.

### Contraintes respectées
- Les classes `// WARN` sont **déplacées sans modification d'un octet** ; leur namespace d'origine (`Refacto.DotNet.Controllers.Services…`) est donc conservé.

### Comportements existants conservés volontairement
Pas de régression, y compris sur ces particularités (figées par des tests) :
- Bornes de saison strictes : le premier et le dernier jour de saison, le produit n'est pas vendu.
- Un produit `EXPIRABLE` en rupture mais non expiré déclenche une notification d'*expiration*.
- Un produit `SEASONAL` en rupture sans délai notifie un délai de 0 jour (un `NORMAL` ne notifie rien).
- Un produit `EXPIRABLE` sans date d'expiration lève une exception.

Seul changement assumé : un produit sans nom est notifié avec un nom vide.

### Limites et pistes
- `INotificationService` (non modifiable) identifie le produit par son **nom**, non unique : un identifiant de produit serait préférable.
- La réponse pourrait détailler le statut de chaque produit (vendu, en délai, indisponible, expiré).

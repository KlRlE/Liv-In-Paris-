INSERT INTO Compte (Id, nom, prénom, téléphone, adresse_mail, numéro, rue, Code_Postal, Ville, MetroLePlusProche, Radié)
VALUES 
    ('U101', 'Durand', 'Mehdy', 1234567890, 'Mdurand@gmail.com', 15, 'Rue Cardinet', 75017, 'Paris', 'Cardinet', FALSE),
    ('U102', 'Dupond', 'Marie', 1234567890, 'Mdupond@gmail.com', 30, 'Rue de la Rép', 75011, 'Paris', 'République', FALSE);

INSERT INTO Client (idClient, Type, Id)
VALUES 
    ('C101', 'Particulier', 'U101');

INSERT INTO Cuisinier (idCuisinier, Type, Id)
VALUES 
    ('CU101', 'Chef', 'U102');
INSERT INTO Commande (idCommande, idClient, Date_Commande, CoutTotal)
VALUES 
    ('CMD001', 'C101', '2025-01-10', 60),
    ('CMD002', 'C101', '2025-01-10', 30);
INSERT INTO Plat (idPlat, Entrée, Plat_Principal, Dessert, NbPersonnes, DateFabrication, Prix, PaysOrigine, RégimeNutritionnel, Ingrédients, Recette)
VALUES 
    ('PLAT001', FALSE, TRUE, FALSE, 10,'2025-01-10', 10.00, 'France', 'Normal', 'Fromage, pommes de terre, jambon, cornichon', 'Raclette'),
    ('PLAT002', FALSE, FALSE, TRUE, 5, '2025-01-10', 6.00, 'France', 'Végétarien', 'Fraise, kiwi, sucre', 'Salade de fruits');

INSERT INTO LigneDeCommande (idLigneDeCommande, idPlat, idCommande, Quantité)
VALUES 
    ('LDC001', 'CMD001', 'PLAT001', 6),
    ('LDC002', 'CMD002', 'PLAT002', 6);

INSERT INTO Cuisine (idCuisinier, idPlat)
VALUES 
    ('CU101', 'PLAT001'),
    ('CU101', 'PLAT002');

INSERT INTO Particulier (idClient, Type)
VALUES ('C101', 'particulier');

INSERT INTO Livraison (idLivraison, idLigneDeCommande, LieuLivraison)
VALUES 
    ('LIV3001', 'LDC001',  '75017, Paris'),
    ('LIV3002', 'LDC002',  '75017, Paris');

INSERT INTO Fait_Retour (idClient, idCuisinier, DateRetour, Commentaire)
VALUES 
    ('C101', 'CU101', '2025-01-15', 'Trop chère');  

    
  SELECT* 
  FROM Plat;
  
  SELECT*
  FROM Client;
   
  SELECT prénom
  FROM Compte
  WHERE prénom LIKE 'M%';
    




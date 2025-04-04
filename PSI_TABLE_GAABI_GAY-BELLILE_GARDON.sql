DROP DATABASE IF EXISTS PSI;
CREATE DATABASE IF NOT EXISTS PSI;
use PSI; 


CREATE TABLE Plat(
   idPlat VARCHAR(50),
   Entrée BOOLEAN,
   Plat_Principal BOOLEAN,
   Dessert BOOLEAN,
   NbPersonnes INT,
   DateFabrication DATETIME,
   Prix DECIMAL(15,2),
   PaysOrigine VARCHAR(50),
   RégimeNutritionnel VARCHAR(50),
   Ingrédients VARCHAR(100),
   Photo VARCHAR(50),
   Recette VARCHAR(50),
   PRIMARY KEY(idPlat)
);

CREATE TABLE Compte(
   Id VARCHAR(50),
   nom VARCHAR(50),
   prénom VARCHAR(50),
   téléphone INT,
   adresse_mail VARCHAR(50),
   numéro INT,
   rue VARCHAR(50),
   Code_Postal INT,
   Ville VARCHAR(50),
   MetroLePlusProche VARCHAR(50),
   Radié BOOLEAN,
   PRIMARY KEY(Id)
);

CREATE TABLE Client(
   idClient VARCHAR(50),
   Type VARCHAR(50),
   Id VARCHAR(50) NOT NULL,
   PRIMARY KEY(idClient),
   UNIQUE(Id),
   FOREIGN KEY(Id) REFERENCES Compte(Id)
);

CREATE TABLE Cuisinier(
   idCuisinier VARCHAR(50),
   Type VARCHAR(50),
   Id VARCHAR(50) NOT NULL,
   PRIMARY KEY(idCuisinier),
   UNIQUE(Id),
   FOREIGN KEY(Id) REFERENCES Compte(Id)
);

CREATE TABLE Commande(
   idCommande VARCHAR(50),
   Date_Commande DATE,
   CoutTotal INT,
   idClient VARCHAR(50) NOT NULL,
   PRIMARY KEY(idCommande),
   FOREIGN KEY(idClient) REFERENCES Client(idClient)
);

CREATE TABLE Entreprise_locale(
   idClient VARCHAR(50),
   nomEntreprise VARCHAR(50) NOT NULL,
   nomRéférent VARCHAR(50),
   Type VARCHAR(50),
   PRIMARY KEY(idClient),
   FOREIGN KEY(idClient) REFERENCES Client(idClient)
);

CREATE TABLE Particulier(
   idClient VARCHAR(50),
   Type VARCHAR(50),
   PRIMARY KEY(idClient),
   FOREIGN KEY(idClient) REFERENCES Client(idClient)
);

CREATE TABLE LigneDeCommande(
   idLigneDeCommande VARCHAR(50),
   Quantité INT,
   idPlat VARCHAR(50) NOT NULL,
   idCommande VARCHAR(50) NOT NULL,
   PRIMARY KEY(idLigneDeCommande),
   FOREIGN KEY(idPlat) REFERENCES Plat(idPlat),
   FOREIGN KEY(idCommande) REFERENCES Commande(idCommande)
);

CREATE TABLE Livraison(
   idLivraison VARCHAR(50),
   LieuLivraison VARCHAR(50),
   idLigneDeCommande VARCHAR(50) NOT NULL,
   PRIMARY KEY(idLivraison),
   FOREIGN KEY(idLigneDeCommande) REFERENCES LigneDeCommande(idLigneDeCommande)
);

CREATE TABLE Fait_Retour(
   idClient VARCHAR(50),
   idCuisinier VARCHAR(50),
   DateRetour DATETIME,
   Commentaire VARCHAR(50),
   PRIMARY KEY(idClient, idCuisinier),
   FOREIGN KEY(idClient) REFERENCES Client(idClient),
   FOREIGN KEY(idCuisinier) REFERENCES Cuisinier(idCuisinier)
);


CREATE TABLE Cuisine(
   idCuisinier VARCHAR(50),
   idPlat VARCHAR(50),
   PRIMARY KEY(idCuisinier, idPlat),
   FOREIGN KEY(idCuisinier) REFERENCES Cuisinier(idCuisinier),
   FOREIGN KEY(idPlat) REFERENCES Plat(idPlat)
);

CREATE TABLE Effectue(
   idCuisinier VARCHAR(50),
   idLivraison VARCHAR(50),
   PRIMARY KEY(idCuisinier, idLivraison),
   FOREIGN KEY(idCuisinier) REFERENCES Cuisinier(idCuisinier),
   FOREIGN KEY(idLivraison) REFERENCES Livraison(idLivraison)
);

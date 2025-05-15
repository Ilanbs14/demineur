# 🎮 Démineur en C# (WPF)

Bienvenue sur mon projet **Démineur**, réalisé en **C# avec WPF**. Il s’agit d’une version graphique du célèbre jeu de logique, avec les fonctionnalités classiques et un système d’enregistrement des meilleurs temps.

## 🧠 Règles du jeu

Le but du jeu est de **révéler toutes les cases** sans cliquer sur une mine 💣. Chaque case vide peut indiquer combien de mines se trouvent autour d’elle. Si une mine est déclenchée, la partie est perdue.

## 🖥️ Fonctionnalités

- Interface graphique intuitive (WPF)
- Choix de la **taille de la grille**
- Choix du **nombre de mines**
- **Chronomètre** pour suivre votre temps
- **Enregistrement du meilleur temps** localement
- Système de **victoire/défaite**
- Réinitialisation de la partie à tout moment
- Gestion des **clics gauche/droit** pour révéler ou poser un drapeau 🚩

## 📸 Aperçu
![demineur](https://github.com/user-attachments/assets/769b3948-797c-44d6-a679-75147d45d766)

## 🛠️ Technologies

- C#
- WPF (XAML)
- .NET Framework

## 🚀 Lancer le projet

1. Ouvre le fichier `Demineur.sln` avec **Visual Studio**.
2. Compile le projet (Ctrl + Maj + B).
3. Lance l'application (F5 ou bouton "Démarrer").

✅ **Pré-requis** : Visual Studio avec support WPF installé.

## 📁 Arborescence du projet

```plaintext
Demineur/
│
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── Partie.cs
├── Case.cs
├── meilleurs_temps.txt (non inclus sur GitHub)
├── Demineur.csproj
└── ...
```
#✍️ Auteur
Ilan Luis
Étudiant en BUT Informatique à l'IUT d'Orsay

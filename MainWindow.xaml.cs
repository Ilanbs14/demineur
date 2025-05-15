using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MinesweeperWPF
{
    public partial class MainWindow : Window
    {
        private int gridSize = 5;
        private int nbMines = 3;
        private int nbCellOpen = 0;
        private int[,] matrix;
        private bool firstClick = true;
        private bool isLoaded = false;
        private int previousSelectedIndex = 0;
        private int bombesRestantes;
        private bool isPaused = false;
        private DispatcherTimer timer;
        private int secondsElapsed;
        private const string SaveFilePath = "meilleurs_temps.txt";

        private void SauvegarderMeilleursTemps()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(SaveFilePath))
                {
                    foreach (var entry in meilleursTemps)
                    {
                        writer.WriteLine($"{entry.Key}:{entry.Value.TotalSeconds}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la sauvegarde des meilleurs temps : " + ex.Message);
            }
        }

        private void ChargerMeilleursTemps()
        {
            try
            {
                if (!File.Exists(SaveFilePath)) return;

                string[] lignes = File.ReadAllLines(SaveFilePath);
                foreach (var ligne in lignes)
                {
                    var parts = ligne.Split(':');
                    if (parts.Length == 2)
                    {
                        string niveau = parts[0];
                        if (double.TryParse(parts[1], out double totalSeconds))
                        {
                            meilleursTemps[niveau] = TimeSpan.FromSeconds(totalSeconds);
                        }
                    }
                }

                // Mettre à jour les affichages
                RunFacileTime.Text = meilleursTemps["facile"] == TimeSpan.MaxValue ? "--:--" : meilleursTemps["facile"].ToString(@"mm\:ss");
                RunMoyenTime.Text = meilleursTemps["moyen"] == TimeSpan.MaxValue ? "--:--" : meilleursTemps["moyen"].ToString(@"mm\:ss");
                RunDifficileTime.Text = meilleursTemps["difficile"] == TimeSpan.MaxValue ? "--:--" : meilleursTemps["difficile"].ToString(@"mm\:ss");
                RunExtremeTime.Text = meilleursTemps["extreme"] == TimeSpan.MaxValue ? "--:--" : meilleursTemps["extreme"].ToString(@"mm\:ss");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des meilleurs temps : " + ex.Message);
            }
        }

        // Pour suivre les meilleurs temps par niveau
        private Dictionary<string, TimeSpan> meilleursTemps = new Dictionary<string, TimeSpan>()
        {
            { "facile", TimeSpan.MaxValue },
            { "moyen", TimeSpan.MaxValue },
            { "difficile", TimeSpan.MaxValue },
            { "extreme", TimeSpan.MaxValue }
        };
        private void MettreAJourMeilleurTemps(string niveau, TimeSpan temps)
        {
            if (meilleursTemps.ContainsKey(niveau) && temps < meilleursTemps[niveau])
            {
                meilleursTemps[niveau] = temps;
                string texteTemps = temps.ToString(@"mm\:ss");

                switch (niveau)
                {
                    case "facile":
                        RunFacileTime.Text = texteTemps;
                        break;
                    case "moyen":
                        RunMoyenTime.Text = texteTemps;
                        break;
                    case "difficile":
                        RunDifficileTime.Text = texteTemps;
                        break;
                    case "extreme":
                        RunExtremeTime.Text = texteTemps;
                        break;
                }
            }
            SauvegarderMeilleursTemps();
        }


        public MainWindow()
        {
            InitializeComponent();
            isLoaded = true;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1); 
            timer.Tick += Timer_Tick;

            // Démarrer le timer
            ChargerMeilleursTemps();
            secondsElapsed = 0; // Initialisation du compteur à 0
            InitializeGame();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Incrémenter le compteur de temps
            secondsElapsed++;

            // Convertir les secondes écoulées en minutes et secondes
            int minutes = secondsElapsed / 60;
            int seconds = secondsElapsed % 60;

            // Mettre à jour le Label avec le nouveau temps
            TimerLabel.Content = $"{minutes:D2}:{seconds:D2}";
        }
        private void ResetTimer()
        {
            // Réinitialiser le compteur et le timer
            secondsElapsed = 0;
            TimerLabel.Content = "00:00";
        }

        private void CBB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isLoaded) return;

            if (!firstClick)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Voulez-vous vraiment changer de niveau ? La partie en cours sera quittée.",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.No)
                {
                    // Rétablir la sélection précédente
                    CBB.SelectionChanged -= CBB_SelectionChanged;
                    CBB.SelectedIndex = previousSelectedIndex;
                    CBB.SelectionChanged += CBB_SelectionChanged;
                    return;
                }
            }

            // Mettre à jour le niveau
            switch (CBB.SelectedIndex)
            {
                case 0: // Facile
                    gridSize = 5;
                    nbMines = 3;
                    break;
                case 1: //Moyen
                    gridSize = 7;
                    nbMines = 12;
                    break;
                case 2: //Difficile
                    gridSize = 10;
                    nbMines = 26;
                    break;
                case 3: //Extrème
                    gridSize = 15;
                    nbMines = 50;
                    break;
                case 4: // Personnalisé
                    CustomSizePanel.Visibility = Visibility.Visible; // Affiche les boutons
                    return;
                default:
                    return;
            }
            if (CBB.SelectedIndex != 4 && CustomSizePanel.Visibility == Visibility.Visible)
            {
                CustomSizePanel.Visibility = Visibility.Collapsed; // Masquer le StackPanel
                GridSizeTextBox.Clear(); // Optionnel : Réinitialiser les champs de saisie
                MinesCountTextBox.Clear(); // Optionnel : Réinitialiser les champs de saisie
            }

            previousSelectedIndex = CBB.SelectedIndex;
            InitializeGame();
        }

        private void InitializeGame()
        {
            firstClick = true;
            matrix = new int[gridSize, gridSize];
            nbCellOpen = 0;
            GRDGame.Children.Clear();
            GRDGame.ColumnDefinitions.Clear();
            GRDGame.RowDefinitions.Clear();

            BTNRelancer.Visibility = Visibility.Collapsed;
            for (int i = 0; i < gridSize; i++)
            {
                GRDGame.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                GRDGame.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            }

            for (int j = 0; j < gridSize; j++)
            {
                for (int i = 0; i < gridSize; i++)
                {
                    Border b = new Border
                    {
                        BorderThickness = new Thickness(1),
                        BorderBrush = new SolidColorBrush(Colors.LightBlue)
                    };
                    b.SetValue(Grid.RowProperty, j);
                    b.SetValue(Grid.ColumnProperty, i);

                    Grid cellGrid = new Grid();

                    Label lbl = new Label
                    {
                        Visibility = Visibility.Hidden,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    Button btn = new Button
                    {
                        Background = Brushes.LightGray
                    };
                    btn.PreviewMouseDown += Button_MouseDown;
                    btn.SetValue(Grid.RowProperty, j);
                    btn.SetValue(Grid.ColumnProperty, i);


                    cellGrid.Children.Add(lbl); // Label en fond
                    cellGrid.Children.Add(btn); // Bouton au-dessus

                    b.Child = cellGrid;
                    GRDGame.Children.Add(b);
                }
            }

            bombesRestantes = nbMines;

            LBLCasesNbr.Content = gridSize * gridSize - nbMines;
            LBLBombesNbr.Content = bombesRestantes;

            ResetTimer();
            timer.Stop(); // Arrêter le timer
            if (isPaused)
            {
                BTNPause_Click(null, null); // Reprendre le jeu si en pause
            }
            BTNPause.Visibility = Visibility.Collapsed; // Masquer le bouton de pause
        }

        private void PlaceMines(int rowExclu, int colExclu)
        {
            Random random = new Random();
            int minesPlaced = 0;

            while (minesPlaced < nbMines)
            {
                int row = random.Next(gridSize);
                int col = random.Next(gridSize);

                if (matrix[row, col] != -1 && !(Math.Abs(row - rowExclu) <= 1 && Math.Abs(col - colExclu) <= 1))
                {
                    matrix[row, col] = -1;
                    minesPlaced++;
                }
            }

            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    if (matrix[row, col] == -1) continue;
                    matrix[row, col] = CountMinesAround(row, col);
                }
            }
        }

        private int CountMinesAround(int row, int col)
        {
            int count = 0;
            for (int i = Math.Max(0, row - 1); i <= Math.Min(gridSize - 1, row + 1); i++)
            {
                for (int j = Math.Max(0, col - 1); j <= Math.Min(gridSize - 1, col + 1); j++)
                {
                    if (matrix[i, j] == -1)
                        count++;
                }
            }
            return count;
        }

        private async void Button_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Button btn = (Button)sender;
            int row = Grid.GetRow(btn);
            int col = Grid.GetColumn(btn);
            if (isPaused)
            {
                return;
            }
            if (firstClick)
            {
                PlaceMines(row, col);
                firstClick = false;
                timer.Start();
            }
            BTNRelancer.Visibility = Visibility.Visible; 
            BTNPause.Visibility = Visibility.Visible;

            if (e.ChangedButton == System.Windows.Input.MouseButton.Right)
            {
                if (btn.Background == Brushes.Red)
                {
                    btn.Background = Brushes.LightGray;
                    bombesRestantes += 1;
                }
                else
                {
                    btn.Background = Brushes.Red;
                    bombesRestantes -= 1;
                }

                e.Handled = true;
                LBLBombesNbr.Content = bombesRestantes;
                return;
            }

            if (btn.Background == Brushes.Red)
            {
                e.Handled = true;
                return;
            }

            if (matrix[row, col] == -1)
            {
                timer.Stop();
                btn.Content = "💣";
                btn.Background = Brushes.Black;
                btn.Foreground = Brushes.Red;

                await RevealAllMines(row, col);
                MessageBox.Show("Vous avez perdu!");

                ResetGame();
            }
            else
            {
                if (matrix[row, col] == 1)
                {
                    btn.Foreground = Brushes.Blue;
                }
                if (matrix[row, col] == 2)
                {
                    btn.Foreground = Brushes.Green;
                }

                if (matrix[row, col] == 3)
                {
                    btn.Foreground = Brushes.Red;
                }

                if (matrix[row, col] == 4)
                {
                    btn.Foreground = Brushes.Purple;
                }
                if (matrix[row, col] == 5)
                {
                    btn.Foreground = Brushes.Yellow;
                }
                if (matrix[row, col] == 6)
                {
                    btn.Foreground = Brushes.Pink;
                }
                if (matrix[row, col] == 7)
                {
                    btn.Foreground = Brushes.DarkBlue;
                }
                RevealCell(row, col);
                CheckWin();
            }

            e.Handled = true;
        }



        private void RevealCell(int row, int col)
        {
            if (row < 0 || col < 0 || row >= gridSize || col >= gridSize)
                return;

            Border border = (Border)GetUIElementFromPosition(GRDGame, col, row);
            Grid cellGrid = (Grid)border.Child;

            Label lbl = (Label)cellGrid.Children[0];
            Button btn = (Button)cellGrid.Children[1];

            if (lbl.Visibility == Visibility.Visible || btn.Visibility == Visibility.Hidden)
                return;

            int mineCount = matrix[row, col];
            lbl.Content = mineCount == 0 ? "" : mineCount.ToString();
            lbl.Visibility = Visibility.Visible;
            btn.Visibility = Visibility.Hidden;

            lbl.FontWeight = FontWeights.Bold;
            lbl.FontSize = 16;

            if (mineCount == 1)
            {
                lbl.Foreground = Brushes.Blue;
            }
            else if (mineCount == 2)
            {
                lbl.Foreground = Brushes.Green;
            }

            else if (mineCount == 3)
            {
                lbl.Foreground = Brushes.Red;
            }

            else if (mineCount == 4)
            {
                lbl.Foreground = Brushes.Purple;
            }
            else if (mineCount == 5)
            {
                lbl.Foreground = Brushes.Yellow;
            }
            else
            {
                lbl.Foreground = Brushes.Black;
            }
            nbCellOpen++;
            LBLCasesNbr.Content = gridSize * gridSize - nbMines - nbCellOpen;

            if (mineCount == 0)
            {
                for (int i = row - 1; i <= row + 1; i++)
                {
                    for (int j = col - 1; j <= col + 1; j++)
                    {
                        if (i == row && j == col) continue;
                        if (i >= 0 && j >= 0 && i < gridSize && j < gridSize)
                            RevealCell(i, j);
                    }
                }
            }
        }

        private async Task RevealAllMines(int rowLose, int colLose)
        {
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    if (matrix[row, col] == -1 && row != rowLose && col != colLose)
                    {
                        Border border = (Border)GetUIElementFromPosition(GRDGame, col, row);
                        Grid cellGrid = (Grid)border.Child;

                        Button btn = (Button)cellGrid.Children[1];
                        btn.Content = "💣";
                        btn.Foreground = (Brush)new BrushConverter().ConvertFromString("#FFC12B2B");
                        btn.IsEnabled = false;

                        await Task.Delay(200); // Pause de 200 ms
                    }
                }
            }
        }


        private UIElement GetUIElementFromPosition(Grid g, int col, int row)
        {
            return g.Children.Cast<UIElement>().First(e => Grid.GetRow(e) == row && Grid.GetColumn(e) == col);
        }

        private void CheckWin()
        {
            int totalSafeCells = gridSize * gridSize - nbMines;
            if (nbCellOpen == totalSafeCells)
            {
                // Calcul du temps écoulé
                TimeSpan tempsPartie = TimeSpan.FromSeconds(secondsElapsed);
                timer.Stop(); // Arrêter le timer
                MessageBox.Show("Bravo ! Vous avez gagné !");

                // Identifier le niveau en cours
                string niveau = "";
                switch (CBB.SelectedIndex)
                {
                    case 0: niveau = "facile"; break;
                    case 1: niveau = "moyen"; break;
                    case 2: niveau = "difficile"; break;
                    case 3: niveau = "extreme"; break;
                    default: return; // On ne met pas à jour pour un niveau personnalisé
                }

                // Mettre à jour le meilleur temps
                MettreAJourMeilleurTemps(niveau, tempsPartie);

                ResetGame();
            }
        }


        private void ResetGame()
        {
            InitializeGame();
        }

        private void BTNRelancer_Click(object sender, RoutedEventArgs e)
        {
            if (!firstClick)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Voulez-vous vraiment relancer la partie ?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    InitializeGame();
                }
            }
        }

        private void ValidateCustomSize(object sender, RoutedEventArgs e)
        {
            int customGridSize;
            int customMinesCount;

            // Essayer de récupérer la taille de la grille et le nombre de mines
            if (int.TryParse(GridSizeTextBox.Text, out customGridSize) && int.TryParse(MinesCountTextBox.Text, out customMinesCount))
            {
                if (customGridSize < 5 || customGridSize > 20) // Limiter la taille de la grille
                {
                    MessageBox.Show("La taille de la grille doit être comprise entre 5 et 20.");
                    return;
                }

                if (customMinesCount < 1 || customMinesCount >= customGridSize * customGridSize) // Limiter le nombre de mines
                {
                    MessageBox.Show("Le nombre de mines doit être un nombre valide pour la grille.");
                    return;
                }

                // Mettre à jour la taille de la grille et le nombre de mines
                gridSize = customGridSize;
                nbMines = customMinesCount;

                InitializeGame();
                CustomSizePanel.Visibility = Visibility.Collapsed; // Masquer à nouveau les champs personnalisés
                CBB.SelectedIndex = 4; // Garder l'option "Personnalisé" sélectionnée
            }
            else
            {
                MessageBox.Show("Veuillez entrer des valeurs valides pour la taille de la grille et le nombre de mines.");
            }
        }

        private void BTNPause_Click(object sender, RoutedEventArgs e)
        {
            isPaused = !isPaused;

            if (isPaused)
            {
                // Mettre le jeu en pause
                timer.Stop();
                BTNPause.Content = "▶️";

                // Désactiver tous les boutons (cases)
                foreach (UIElement element in GRDGame.Children)
                {
                    if (element is Border border && border.Child is Grid cellGrid)
                    {
                        foreach (UIElement child in cellGrid.Children)
                        {
                            if (child is Button btn)
                            {
                                btn.IsEnabled = false;
                            }
                        }
                    }
                }
            }
            else
            {
                // Reprendre le jeu
                timer.Start();
                BTNPause.Content = "⏸️";

                // Réactiver tous les boutons (cases)
                foreach (UIElement element in GRDGame.Children)
                {
                    if (element is Border border && border.Child is Grid cellGrid)
                    {
                        foreach (UIElement child in cellGrid.Children)
                        {
                            if (child is Button btn && btn.Visibility == Visibility.Visible)
                            {
                                btn.IsEnabled = true;
                            }
                        }
                    }
                }
            }
        }

        private void BTNMeilleursTemps_Click(object sender, RoutedEventArgs e)
        {
            if (PanelBestTime.Visibility == Visibility.Visible)
            {
                PanelBestTime.Visibility = Visibility.Collapsed;
            }
            else
            {
                PanelBestTime.Visibility = Visibility.Visible;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SeaWar
{
    public partial class Form1 : Form
    {
        private int x = 10;            // Initial X coordinate
        private int y = 10;            // Initial Y coordinate
        private int buttonSize = 25;   // Button size
        private int spacing = 1;       // Spacing between buttons
        private int rowCount = 11;     // Number of rows (10 + 1 for headers)
        private int columnCount = 11;  // Number of columns (10 + 1 for headers)

        private Button[,] buttons1;
        private Button[,] buttons2;
        private int[] shipSizes = { 1, 2, 3, 4 };
        private int[] shipCounts = { 4, 3, 2, 1 };
        private int[] currentShipCounts = { 0, 0, 0, 0 };
        private int selectedRadioIndex = -1;

        private Dictionary<Button, (int row, int col)> placedButtons;
        private List<(int row, int col)> currentShip;
        private Dictionary<int, List<(int row, int col)>> shipsPerMode;
        private List<RadioButton> radioButtons; // Список для зберігання радіо кнопок
        private List<List<(int row, int col)>> botShips; // Список для зберігання кораблів бота

        private bool buildingShips = true;

        private List<List<(int row, int col)>> playerShips; // Список для зберігання кораблів гравця

        private Random rand = new Random(); // Random object for bot's shots

        public Form1()
        {
            InitializeComponent();

            placedButtons = new Dictionary<Button, (int row, int col)>();
            currentShip = new List<(int row, int col)>();
            radioButtons = new List<RadioButton>(); // Ініціалізація списку радіо кнопок
            botShips = new List<List<(int row, int col)>>(); // Ініціалізація списку кораблів бота

            playerShips = new List<List<(int row, int col)>>();

            // Create the first field
            buttons1 = new Button[rowCount, columnCount];
            CreateMaps(this, x, y, buttonSize, spacing, rowCount, columnCount, buttons1);

            // Create the second field to the right of the first one
            int secondFieldX = x + (columnCount * (buttonSize + spacing)) + 20;
            buttons2 = new Button[rowCount, columnCount];
            CreateMaps(this, secondFieldX, y, buttonSize, spacing, rowCount, columnCount, buttons2);

            // Create the radio buttons to the right of the second field
            string[] radioButtonNames = { "■ X 4", "■■ X 3", "■■■ X 2", "■■■■ X 1" };
            CreateRadioButtons(this, secondFieldX + (columnCount * (buttonSize + spacing)) + 20, y, radioButtonNames);

            // Create the Start Game button below the fields
            CreateStartGameButton(this, 10, y + (rowCount * (buttonSize + spacing)) + 20);

            shipsPerMode = new Dictionary<int, List<(int row, int col)>>();
            for (int i = 0; i < shipSizes.Length; i++)
            {
                shipsPerMode[i] = new List<(int row, int col)>();
            }
        }

        public void CreateMaps(Form form, int x, int y, int buttonSize, int spacing, int rowCount, int columnCount, Button[,] buttons)
        {
            char[] alphabet = "ABCDEFGHIJ".ToCharArray();
            for (int row = 0; row < rowCount; row++)
            {
                for (int column = 0; column < columnCount; column++)
                {
                    Button b = new Button();
                    if (row == 0 && column == 0)
                    {
                        b.Text = "П";
                        b.BackColor = Color.White;
                        b.Enabled = false;
                    }
                    else if (row == 0)
                    {
                        b.Text = alphabet[column - 1].ToString(); // Зміна: використання букв по горизонталі
                        b.BackColor = Color.White;
                        b.Enabled = false;
                    }
                    else if (column == 0)
                    {
                        b.Text = row.ToString(); // Зміна: використання цифр по вертикалі
                        b.BackColor = Color.White;
                        b.Enabled = false;
                    }
                    else
                    {
                        b.Enabled = false;
                        int r = row - 1;
                        int c = column - 1;
                        b.Click += (sender, e) => FieldButton_Click(sender, e, r, c, buttons);
                        b.BackColor = Color.White;
                    }

                    b.Font = new Font(b.Font.FontFamily, 7, FontStyle.Bold);
                    b.Location = new Point(x + column * (buttonSize + spacing), y + row * (buttonSize + spacing));
                    b.Height = buttonSize;
                    b.Width = buttonSize;

                    buttons[row, column] = b; // Зберігати посилання на кнопку в масиві
                    form.Controls.Add(b);
                }
            }
        }


        public void CreateRadioButtons(Form form, int x, int y, string[] names)
        {
            int radioButtonCount = names.Length;
            int radioButtonHeight = 30; // Висота кожного перемикача
            int radioButtonSpacing = 10; // Відстань між перемикачами

            for (int i = 0; i < radioButtonCount; i++)
            {
                RadioButton rb = new RadioButton();
                rb.Text = names[i];
                rb.Location = new Point(x, y + i * (radioButtonHeight + radioButtonSpacing));
                rb.Height = radioButtonHeight;
                rb.Width = 100; // Ширина перемикача
                int index = i; // Індекс захоплення для лямбда-виразу
                rb.CheckedChanged += (sender, e) => RadioButton_CheckedChanged(sender, e, index);
                form.Controls.Add(rb);
                radioButtons.Add(rb); // Додавання радіо кнопки до списку
            }
        }

        public void CreateStartGameButton(Form form, int x, int y)
        {
            Button startGameButton = new Button();
            startGameButton.Text = "Почати гру!";
            startGameButton.Location = new Point(x, y);
            startGameButton.Width = 730;
            startGameButton.Height = 50;
            startGameButton.Click += StartGameButton_Click;
            form.Controls.Add(startGameButton);
        }

        private void RadioButton_CheckedChanged(object? sender, EventArgs e, int index)
        {
            if (sender is RadioButton radioButton && radioButton.Checked)
            {
                selectedRadioIndex = index;
                // Увімкніть усі кнопки в першому полі
                for (int row = 1; row < rowCount; row++)
                {
                    for (int column = 1; column < columnCount; column++)
                    {
                        if (!placedButtons.ContainsKey(buttons1[row, column]))
                        {
                            selectedRadioIndex = index;
                            RemoveCurrentModeShips(); // Видаляємо кораблі поточного режиму
                            buttons1[row, column].Enabled = true;
                        }
                    }
                }
            }
        }

        private void FieldButton_Click(object? sender, EventArgs e, int row, int column, Button[,] buttons)
        {
            if (buildingShips)
            {
                // Переконуємося, що вибрано дійсний індекс перемикача
                if (selectedRadioIndex == -1 || selectedRadioIndex >= shipSizes.Length)
                {
                    return;
                }

                if (sender is Button button)
                {
                    if (currentShip.Contains((row, column)))
                    {
                        // Повернуть кнопку до початкового стану
                        button.BackColor = Color.White;
                        currentShip.Remove((row, column));
                    }
                    else
                    {
                        // Перевіряємо, чи дійсне додавання цієї клітинки та чи не досягнуто максимального розміру корабля
                        if (currentShip.Count < shipSizes[selectedRadioIndex] && IsValidPlacement(row, column))
                        {
                            button.BackColor = Color.Green;
                            currentShip.Add((row, column));
                        }
                    }

                    // Коли поточний корабель досягне вибраного розміру, зберігаємо його та скидуємо для наступного корабля
                    if (currentShip.Count == shipSizes[selectedRadioIndex])
                    {
                        foreach (var (r, c) in currentShip)
                        {
                            placedButtons[buttons[r + 1, c + 1]] = (r, c);
                            shipsPerMode[selectedRadioIndex].Add((r, c)); // Зберігаємо координати корабля
                            playerShips.Add(new List<(int row, int col)> { (r, c) }); // Вносимо координати корабля до списку
                        }
                        currentShipCounts[selectedRadioIndex]++;
                        currentShip.Clear();

                        // Перевіряємо, чи всі кораблі поточного розміру розміщені
                        if (currentShipCounts[selectedRadioIndex] == shipCounts[selectedRadioIndex])
                        {
                            MessageBox.Show($"Всі кораблі цього розміру вже розташовані.");
                            selectedRadioIndex = -1; // Відновлюємо індекс
                        }
                    }
                }
            }
            else
            {
                // Код стрільби для гравця
                if (sender is Button button)
                {
                    // Вистріл для гравця
                    if (buttons == buttons2)
                    {
                        if (button.BackColor == Color.Blue || button.BackColor == Color.Red)
                        {

                        }
                        else
                        {
                            button.BackColor = Color.Blue; // Попав
                            foreach (var ship in botShips)
                            {
                                foreach (var part in ship)
                                {
                                    if (part == (row, column))
                                    {
                                        button.BackColor = Color.Red; 
                                        ship.Remove(part);
                                        if (ship.Count == 0)
                                        {
                                            botShips.Remove(ship);
                                            break;
                                        }
                                    }
                                }
                                if (button.BackColor == Color.Red)
                                    break;
                            }

                            // Перевіряємо мапу бота 
                            if (botShips.Count == 0)
                            {
                                MessageBox.Show("Щиро вітаю! Ви виграли гру!");
                                RestartGame(buttons);
                                return;
                            }

                            if (button.BackColor == Color.Blue) // Якщо промахнувся
                            {
                                BotTurn(buttons);
                            }
                        }
                    }
                }
            }
        }



        private bool IsValidPlacement(int row, int column)
        {
            if (placedButtons.Values.Any(p => Math.Abs(p.row - row) <= 1 && Math.Abs(p.col - column) <= 1))
            {
                return false;
            }

            if (currentShip.Count > 0)
            {
                bool isHorizontal = currentShip[0].row == row;
                bool isVertical = currentShip[0].col == column;

                if (!isHorizontal && !isVertical)
                {
                    return false;
                }

                if (isHorizontal && row == currentShip[0].row)
                {
                    return Math.Abs(currentShip[0].col - column) == 1 || currentShip.Any(p => Math.Abs(p.col - column) == 1);
                }
                if (isVertical && column == currentShip[0].col)
                {
                    return Math.Abs(currentShip[0].row - row) == 1 || currentShip.Any(p => Math.Abs(p.row - row) == 1);
                }
                return false;
            }

            // Переконайтеся, що перше розміщення дійсне
            return true;
        }

        private void RemoveCurrentModeShips()
        {
            if (selectedRadioIndex != -1)
            {
                foreach (var (row, col) in shipsPerMode[selectedRadioIndex])
                {
                    var button = buttons1[row + 1, col + 1];
                    button.BackColor = Color.White;
                    button.Enabled = true;
                    placedButtons.Remove(button);
                }

                shipsPerMode[selectedRadioIndex].Clear();
                currentShipCounts[selectedRadioIndex] = 0;
            }
        }

        private void StartGameButton_Click(object? sender, EventArgs e)
        {
            if (buildingShips)
            {
                // Перевіряємо розташування корабля перед початком гри
                for (int i = 0; i < shipSizes.Length; i++)
                {
                    if (currentShipCounts[i] != shipCounts[i])
                    {
                        MessageBox.Show($"Розташуйте будь ласка всі кораблі!");
                        return;
                    }
                }
                PlaceShipsForBot();

                // Змінюємо режим на стрільбу
                buildingShips = false;

                BotField(buttons2);

                // Вимикаємо перемикачи та відкриваємо друге поле
                foreach (var radioButton in radioButtons)
                {
                    radioButton.Enabled = false;
                }

                for (int row = 1; row < rowCount; row++)
                {
                    for (int column = 1; column < columnCount; column++)
                    {
                        buttons2[row, column].Enabled = true;
                    }
                }


            }
        }

        private void PlaceShipsForBot()
        {
            botShips.Clear();

            Random rand = new Random();
            List<(int row, int col)> availablePositions = new List<(int row, int col)>();

            // Заповнення списку доступних позицій
            for (int row = 1; row < rowCount; row++)
            {
                for (int col = 1; col < columnCount; col++)
                {
                    availablePositions.Add((row, col));
                }
            }

            for (int i = shipSizes.Length - 1; i >= 0; i--)
            {
                int size = shipSizes[i];
                int count = shipCounts[i];

                for (int j = 0; j < count; j++)
                {
                    bool placed = false;
                    int attempts = 0;

                    while (!placed && attempts < 100)
                    {
                        int index = rand.Next(availablePositions.Count);
                        (int row, int col) = availablePositions[index];
                        bool horizontal = rand.Next(2) == 0;

                        if (CanPlaceShip(row, col, size, horizontal, buttons2))
                        {
                            List<(int row, int col)> ship = new List<(int row, int col)>();
                            for (int k = 0; k < size; k++)
                            {
                                int r = horizontal ? row : row + k;
                                int c = horizontal ? col + k : col;
                                buttons2[r, c].BackColor = Color.Green;
                                ship.Add((r, c));
                                botShips.Add(new List<(int row, int col)> { (r - 1, c - 1) });
                            }

                            placed = true;

                            foreach (var (r, c) in ship)
                            {
                                availablePositions.RemoveAll(p => Math.Abs(p.row - r) <= 1 && Math.Abs(p.col - c) <= 1);
                            }
                        }
                        attempts++;
                    }

                    if (!placed)
                    {
                        MessageBox.Show("Не вдалося розмістити всі кораблі для бота. Спробуйте ще раз.");
                        return;
                    }
                }
            }
        }

        private bool CanPlaceShip(int row, int col, int size, bool horizontal, Button[,] buttons)
        {
            for (int i = 0; i < size; i++)
            {
                int r = horizontal ? row : row + i;
                int c = horizontal ? col + i : col;

                if (r >= rowCount || c >= columnCount || buttons[r, c].BackColor == Color.Green)
                {
                    return false;
                }

                // Check adjacent cells
                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        int nr = r + dr;
                        int nc = c + dc;

                        if (nr >= 1 && nr < rowCount && nc >= 1 && nc < columnCount && buttons[nr, nc].BackColor == Color.Green)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        // Додаємо новий метод для очищення поля бота
        private void BotField(Button[,] buttons)
        {
            for (int row = 1; row < rowCount; row++)
            {
                for (int column = 1; column < columnCount; column++)
                {
                    buttons[row, column].BackColor = Color.White;
                }
            }
        }

        private void BotTurn(Button[,] buttons)
        {
            bool botHits = true;
            while (botHits)
            {
                int row = rand.Next(1, rowCount);
                int col = rand.Next(1, columnCount);
                var button = buttons1[row, col];
                if (button.BackColor == Color.Blue || button.BackColor == Color.Red)
                {
                    continue; // Пропустити вже обстріляні клітинки
                }

                button.BackColor = Color.Blue; // Промах бота за замовчуванням

                foreach (var ship in playerShips)
                {
                    foreach (var part in ship)
                    {
                        if (part == (row - 1, col - 1))
                        {
                            button.BackColor = Color.Red; // Попадання бота
                            ship.Remove(part);
                            if (ship.Count == 0)
                            {
                                playerShips.Remove(ship);
                                break;
                            }
                        }
                    }
                    if (button.BackColor == Color.Red)
                        break;
                }

                // Перевірка, чи гравець програв
                if (playerShips.Count == 0)
                {
                    MessageBox.Show("На жаль, ви програли гру!");
                    RestartGame(buttons);
                    return;
                }

                if (button.BackColor == Color.Blue)
                {
                    botHits = false; // Бот промахнувся, перехід до ходу гравця
                }
            }
        }

        private void RestartGame(Button[,] buttons)
        {
            for (int row = 1; row < rowCount; row++)
            {
                for (int column = 1; column < columnCount; column++)
                {
                    // Очистити карти
                    ClearMap(buttons1);
                    ClearMap(buttons2);

                    // Очистити змінні
                    placedButtons.Clear();
                    currentShip.Clear();
                    for (int i = 0; i < shipSizes.Length; i++)
                    {
                        shipsPerMode[i].Clear();
                        currentShipCounts[i] = 0;
                    }
                    selectedRadioIndex = -1;
                    botShips.Clear();
                    playerShips.Clear();
                    buildingShips = true;

                    buttons2[row, column].Enabled = false;
                    buttons[row, column].BackColor = Color.White;

                    // Увімкнути всі радіо кнопки
                    foreach (var radioButton in radioButtons)
                    {
                        radioButton.Enabled = true;
                        radioButton.Checked = false;
                    }
                }
            }

            MessageBox.Show("Гру перезапущено! Розміщуйте свої кораблі, щоб почати.");
        }




        private void ClearMap(Button[,] buttons)
        {
            for (int row = 1; row < rowCount; row++)
            {
                for (int column = 1; column < columnCount; column++)
                {
                    buttons[row, column].BackColor = Color.White;
                    buttons2[row, column].Enabled = false;
                }
            }
        }
    }
}

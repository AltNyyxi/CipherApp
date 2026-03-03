using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace CipherApp
{
    public partial class MainWindow : Window
    {
        private struct LiqvidSymbols
        {
            public char c;  
            public int pos;  

            public LiqvidSymbols(char c, int pos)
            {
                this.c = c;
                this.pos = pos;
            }
        }

        private const string RUS_ALPHABET_UPPER = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
        private const string RUS_ALPHABET_LOWER = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";
        private List<string> methods = new List<string> { "Столбцовый метод", "Метод Виженера" };
        List<LiqvidSymbols> liqv_symbols = new List<LiqvidSymbols>();

        public MainWindow()
        {
            InitializeComponent();
            MethodCombo.ItemsSource = methods;
            MethodCombo.SelectedIndex = 0;
        }

        private string NormalizeText(string text, bool collectInvalidSymbols = false)
        {
            if (collectInvalidSymbols)
            {
                liqv_symbols.Clear();
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (RUS_ALPHABET_UPPER.IndexOf(c) >= 0 || RUS_ALPHABET_LOWER.IndexOf(c) >= 0)
                {
                    sb.Append(char.ToUpper(c));
                }
                else if (collectInvalidSymbols)
                {
                    liqv_symbols.Add(new LiqvidSymbols(c, i));
                }
            }
            return sb.ToString();
        }

        private string RestoreInvalidSymbols(string text)
        {
            if (liqv_symbols.Count == 0)
                return text;

            char[] result = text.ToCharArray();
            List<char> resultList = new List<char>(result);

            foreach (var symbol in liqv_symbols)
            {
                if (symbol.pos <= resultList.Count)
                {
                    resultList.Insert(symbol.pos, symbol.c);
                }
                else
                {
                    resultList.Add(symbol.c);
                }
            }

            return new string(resultList.ToArray());
        }

        private string NormalizeKeyText(string text)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in text)
            {
                if (RUS_ALPHABET_UPPER.IndexOf(c) >= 0 || RUS_ALPHABET_LOWER.IndexOf(c) >= 0)
                {
                    sb.Append(char.ToUpper(c));
                }
            }
            return sb.ToString();
        }

        private string NormalizeTextChange(string text)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in text)
            {
                if (RUS_ALPHABET_LOWER.IndexOf(c) >= 0)
                {
                    sb.Append(char.ToUpper(c));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private void KeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;
            int caretIndex = tb.CaretIndex;
            string cleaned = NormalizeTextChange(tb.Text);
            if (cleaned == tb.Text) return;
            tb.Text = cleaned;
            tb.CaretIndex = Math.Min(caretIndex, cleaned.Length);
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;
            int caretIndex = tb.CaretIndex;
            string cleaned = NormalizeTextChange(tb.Text);
            if (cleaned == tb.Text) return;
            tb.Text = cleaned;
            tb.CaretIndex = Math.Min(caretIndex, cleaned.Length);
        }

        private string ColumnarTransposEncipher(string plaintext, string key)
        {
            plaintext = NormalizeText(plaintext, true);
            key = NormalizeKeyText(key);

            if (string.IsNullOrEmpty(key)) 
            {
                MessageBox.Show("Для начала введите ключ состоящий из валидных символов");
                return ""; 
            }
            if (string.IsNullOrEmpty(plaintext))
            {
                MessageBox.Show("Для начала введите исходный текст состоящий из валидных символов");
                return "";
            }

            int keySize = key.Length;

            var columns = new List<(int index, char keyChar)>();
            for (int i = 0; i < keySize; i++)
            {
                columns.Add((i, key[i]));
            }

            var sortedColumns = columns
                .OrderBy(c => RUS_ALPHABET_UPPER.IndexOf(c.keyChar))
                .ThenBy(c => c.index)
                .Select(c => c.index)
                .ToList();

            StringBuilder ciphertext = new StringBuilder();
            foreach (int col in sortedColumns)
            {
                int index = col;
                while (index < plaintext.Length)
                {
                    ciphertext.Append(plaintext[index]);
                    index += keySize;
                }
            }

            return RestoreInvalidSymbols(ciphertext.ToString());
        }

        private string ColumnarTransposDecipher(string ciphertext, string key)
        {

            ciphertext = NormalizeText(ciphertext, true);
            key = NormalizeKeyText(key);

            if (string.IsNullOrEmpty(key))
            {
                MessageBox.Show("Для начала введите ключ состоящий из валидных символов");
                return "";
            }
            if (string.IsNullOrEmpty(ciphertext))
            {
                MessageBox.Show("Для начала введите исходный текст состоящий из валидных символов");
                return "";
            }

            int keySize = key.Length;
            int len = ciphertext.Length;
            if (len == 0) return "";

            int rows = (len + keySize - 1) / keySize;
            int remainder = len % keySize;

            var columns = new List<(int index, char keyChar)>();
            for (int i = 0; i < keySize; i++)
            {
                columns.Add((i, key[i]));
            }

            var sortedColumns = columns
                .OrderBy(c => RUS_ALPHABET_UPPER.IndexOf(c.keyChar))
                .ThenBy(c => c.index)
                .Select(c => c.index)
                .ToList();


            char[,] matrix = new char[rows, keySize];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < keySize; c++)
                {
                    matrix[r, c] = ' ';
                }
            }

            int cipherPos = 0;
            foreach (int colIndex in sortedColumns)
            {
                int columnHeight = (remainder == 0 || colIndex < remainder) ? rows : rows - 1;
                for (int row = 0; row < columnHeight; row++)
                {
                    matrix[row, colIndex] = ciphertext[cipherPos++];
                }
            }

            StringBuilder plaintext = new StringBuilder();
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < keySize; col++)
                {
                    if (matrix[row, col] != ' ')
                    {
                        plaintext.Append(matrix[row, col]);
                    }
                }
            }
            return RestoreInvalidSymbols(plaintext.ToString());
        }

        private string VigenereEncipher(string plaintext, string key)
        {
            plaintext = NormalizeText(plaintext, true);
            key = NormalizeKeyText(key);

            if (string.IsNullOrEmpty(key))
            {
                MessageBox.Show("Для начала введите ключ состоящий из валидных символов");
                return "";
            }
            if (string.IsNullOrEmpty(plaintext))
            {
                MessageBox.Show("Для начала введите исходный текст состоящий из валидных символов");
                return "";
            }

            if (plaintext.Length < key.Length)
            {
                key = key.Substring(0, plaintext.Length);
            }
            else if (plaintext.Length > key.Length)
            {
                key += plaintext.Substring(0, plaintext.Length - key.Length);
            }

            StringBuilder ciphertext = new StringBuilder();
            for (int i = 0; i < plaintext.Length; i++)
            {
                int pIdx = RUS_ALPHABET_UPPER.IndexOf(plaintext[i]);
                int kIdx = RUS_ALPHABET_UPPER.IndexOf(key[i]);
                int cIdx = (pIdx + kIdx) % 33;
                ciphertext.Append(RUS_ALPHABET_UPPER[cIdx]);
            }

            return RestoreInvalidSymbols(ciphertext.ToString());
        }

        private string VigenereDecipher(string ciphertext, string key)
        {
            ciphertext = NormalizeText(ciphertext, true);
            key = NormalizeKeyText(key);

            if (string.IsNullOrEmpty(key))
            {
                MessageBox.Show("Для начала введите ключ состоящий из валидных символов");
                return "";
            }
            if (string.IsNullOrEmpty(ciphertext))
            {
                MessageBox.Show("Для начала введите исходный текст состоящий из валидных символов");
                return "";
            }

            StringBuilder plaintext = new StringBuilder();
            string currentKey = key;
            int j = 0;

            for (int i = 0; i < ciphertext.Length; i++)
            {
                char k = currentKey[j];
                int cIdx = RUS_ALPHABET_UPPER.IndexOf(ciphertext[i]);
                int kIdx = RUS_ALPHABET_UPPER.IndexOf(k);
                int pIdx = (cIdx - kIdx + 33) % 33;
                char p = RUS_ALPHABET_UPPER[pIdx];
                plaintext.Append(p);
                currentKey += p;
                j++;
            }

            return RestoreInvalidSymbols(plaintext.ToString());
        }

        private void ReadFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    InputTextBox.Text = File.ReadAllText(openFileDialog.FileName, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при чтении файла: " + ex.Message);
                }
            }
        }

        private void Info_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, ResultTextBox.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при чтении файла: " + ex.Message);
                }
            }
        }

        private void Encrypt_Click(object sender, RoutedEventArgs e)
        {
            string input = InputTextBox.Text;
            string key = KeyTextBox.Text;
            string selectedMethod = MethodCombo.SelectedItem as string;
            string result = "";

            if (selectedMethod == "Столбцовый метод")
            {
                result = ColumnarTransposEncipher(input, key);
            }
            else if (selectedMethod == "Метод Виженера")
            {
                result = VigenereEncipher(input, key);
            }

            ResultTextBox.Text = result;
        }

        private void Decrypt_Click(object sender, RoutedEventArgs e)
        {
            string input = InputTextBox.Text;
            string key = KeyTextBox.Text;
            string selectedMethod = MethodCombo.SelectedItem as string;
            string result = "";

            if (selectedMethod == "Столбцовый метод")
            {
                result = ColumnarTransposDecipher(input, key);
            }
            else if (selectedMethod == "Метод Виженера")
            {
                result = VigenereDecipher(input, key);
            }

            ResultTextBox.Text = result;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = "";
            ResultTextBox.Text = "";
            KeyTextBox.Text = "";
            liqv_symbols.Clear(); 
        }
    }
}
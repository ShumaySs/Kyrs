using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;

namespace ArithmeticExpressionAnalyzer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void ParseExpressions(string[] lines)
        {
            // Очистка DataGridView
            dgvLexemes.Columns.Clear();
            dgvLexemes.Columns.Add("LexemeType", "Тип лексемы");
            dgvLexemes.Columns.Add("Value", "Значение");
            dgvLexemes.Columns.Add("LineNumber", "Строка");
            dgvLexemes.Rows.Clear();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue; // Пропуск пустых строк

                try
                {
                    ValidateLine(line, i + 1); // Проверка строки на ошибки
                    ParseLine(line, i + 1);   // Разбор строки на токены
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка в строке {i + 1}: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ValidateLine(string line, int lineNumber)
        {
            // Проверка: лишние или пропущенные символы
            if (line.Count(c => c == '(') != line.Count(c => c == ')'))
            {
                throw new Exception("Несоответствие количества открывающих и закрывающих скобок.");
            }

            if (Regex.IsMatch(line, @"[^\w\d\s+\-*/();:=0x]"))
            {
                throw new Exception("Строка содержит недопустимые символы.");
            }

            // Проверка: несколько знаков присваивания
            if (Regex.Matches(line, ":=").Count > 1)
            {
                throw new Exception("Несколько операторов присваивания в одной строке.");
            }

            // Проверка: присваивание должно быть корректным
            var assignmentMatch = Regex.Match(line, @"(^|\s)([a-zA-Z_]\w*)\s*:=\s*");
            if (!assignmentMatch.Success && line.Contains(":="))
            {
                throw new Exception("Перед оператором присваивания должен быть идентификатор.");
            }

            // Проверка: неверное шестнадцатеричное число
            if (Regex.IsMatch(line, @"0x[^\da-fA-F]"))
            {
                throw new Exception("Некорректное шестнадцатеричное число.");
            }

            // Проверка: отсутствие продолжения выражения после оператора
            if (Regex.IsMatch(line, @"[+\-*/(]\s*(;|$)"))
            {
                throw new Exception("Отсутствует операнд после оператора.");
            }

            // Проверка: недопустимое окончание строки
            if (Regex.IsMatch(line, @".+[+\-*/]$"))
            {
                throw new Exception("Выражение не может заканчиваться оператором.");
            }
        }
        private void ValidateTokens(List<string> tokens, int lineNumber)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];

                // Проверка, что перед ":=" находится корректный идентификатор
                if (token == ":=")
                {
                    if (i == 0 || GetLexemeType(tokens[i - 1]) != "Идентификатор")
                    {
                        throw new Exception($"Ошибка в строке {lineNumber}: Перед оператором присваивания должен быть идентификатор.");
                    }
                }

                // Проверка на некорректный оператор (например, :+=)
                if (token.StartsWith(":") && token != ":=")
                {
                    throw new Exception($"Ошибка в строке {lineNumber}: Некорректный оператор \"{token}\".");
                }
            }
        }

        private void ParseLine(string line, int lineNumber)
        {
            string pattern = @"(:=|0x[0-9A-Fa-f]+|[a-zA-Z_]\w*|\d+|[+\-*/();])";
            var matches = Regex.Matches(line, pattern);

            List<string> tokens = matches.Cast<Match>().Select(m => m.Value).ToList();

            ValidateTokens(tokens, lineNumber);

            foreach (string token in tokens)
            {
                string lexemeType = GetLexemeType(token);

                if (lexemeType == "Неизвестный символ")
                {
                    throw new Exception($"Неизвестный символ: {token}");
                }

                dgvLexemes.Rows.Add(lexemeType, token, lineNumber);
            }
        }


        private string GetLexemeType(string token)
        {
            if (token == ":=")
                return "Присваивание";
            if ("+-*/".Contains(token))
                return "Операция";
            if (token.StartsWith("0x") && int.TryParse(token.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out _))
                return "Шестнадцатеричное число";
            if (int.TryParse(token, out _))
                return "Целое число";
            if (Regex.IsMatch(token, @"^[a-zA-Z_][\w]*$")) // Проверка на корректность идентификатора
                return "Идентификатор";
            if (token == ";")
                return "Конец выражения";
            if ("()".Contains(token))
                return "Скобка";

            return "Неизвестный символ";
        }

        private void btnLoadFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt",
                Title = "Выберите файл с выражениями"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string[] lines = File.ReadAllLines(openFileDialog.FileName);

                    if (lines.Length == 0)
                    {
                        MessageBox.Show("Файл пуст.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    txtExpressions.Text = string.Join(Environment.NewLine, lines);
                    ParseExpressions(lines);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}

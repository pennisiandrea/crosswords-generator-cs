namespace CrosswordGeneratorCS
{
    //from item in _cells where item._column >= tmp_column && item._column <= tmp_column+tmp_len select item
    public class Crossword
    {
        private static readonly int MAX_WORD_LEN = 6;
        private static readonly int BLACK_CELL_PERCENTAGE = 20;
        private static readonly char EMPTY_CELL = '_';
        private static readonly char BLACK_CELL = '#';
        private static readonly int MIN_COLUMN_VALUE = 5;
        private static readonly int MAX_COLUMN_VALUE = 30;
        private static readonly int MIN_ROW_VALUE = 5;
        private static readonly int MAX_ROW_VALUE = 30;

        private int _column_num;
        private int _row_num;
        private List<Cell> _cells = new();
        private List<Row> _rows = new();
        private List<Column> _columns = new();
        private static Random _rand = new();
        private List<char> _alphabet = new List<char>{'A','B','C','D','E','F','G','H','I','L','M','N','O','P','Q','R','S','T','U','V','Z'};
        public List<string> _dictionary = new();
        private string _dictionary_file;
        private string _user_word;
        private Cell _user_word_cell;
        private DateTime _generate_start_dt;
        private int _failing_r_idx = 0;
        private int _failing_w_idx = 0;
        private int _timeout = 0;

        private class CrosswordInvalidNumberOfColumns : Exception
        { 
            public CrosswordInvalidNumberOfColumns(int column)
                : base("Crossword main object rejects " + column.ToString() + " as columns number because it is out the allowed values range: (" + MIN_COLUMN_VALUE.ToString() + "," + MAX_COLUMN_VALUE.ToString() + ")") {}
        }
        private class CrosswordInvalidNumberOfRows : Exception
        { 
            public CrosswordInvalidNumberOfRows(int row)
                : base("Crossword main object rejects " + row.ToString() + " as rows number because it is out the allowed values range: (" + MIN_ROW_VALUE.ToString() + "," + MAX_ROW_VALUE.ToString() + ")") {}
        }
        private class CrosswordInvalidDictionaryFile : Exception
        { 
            public CrosswordInvalidDictionaryFile(string file)
                : base("Crossword main object rejects " + file + " as dictionary file.") {}
        }
        private class CrosswordInvalidUserWord : Exception
        { 
            public CrosswordInvalidUserWord(string user_word)
                : base("Crossword main object rejects " + user_word + " because its length is not compatible with columns num or it contains non-italian letters.") {}
        }
        private class CrosswordInvalidColumnAssignedToCell : Exception
        { 
            public CrosswordInvalidColumnAssignedToCell(int column) : base("Cell rejected " + column.ToString() + " as column value because it is not an allowed value.") {}
        }
        private class CrosswordInvalidRowAssignedToCell : Exception
        { 
            public CrosswordInvalidRowAssignedToCell(int row) : base("Cell rejected " + row.ToString() + " as row value because it is not an allowed value.") {}
        }
        private class CrosswordInvalidValAssignedToCell : Exception
        { 
            public CrosswordInvalidValAssignedToCell(char val) : base("Cell rejected " + val + " as cell value because it is not an allowed value.") {}
        }
        private class CrosswordInvalidCellsAssignedToWord : Exception
        { 
            public CrosswordInvalidCellsAssignedToWord() : base() {}
        }
        private class CrosswordInvalidCellTestedOnWord : Exception
        { 
            public CrosswordInvalidCellTestedOnWord(Cell cell, Word word) : base("Trying to test cell (c,r)=(" + cell._column + "," + cell._row + ") with word (first_cell_c,first_cell_r,len,word)=(" + word._first_cell._column + "," + word._first_cell._row + "," + word._len + "," + word.word + ")") {}
        }
        private class CrosswordTryingToOverwriteAlternativeWords : Exception
        { 
            public CrosswordTryingToOverwriteAlternativeWords() : base() {}
        }
        private class CrosswordInvalidCellsAssignedToColumn : Exception
        { 
            public CrosswordInvalidCellsAssignedToColumn(int col) : base("Column " + col.ToString() + " rejected assigned cells.") {}
        }
        private class CrosswordInvalidCellsAssignedToRow : Exception
        { 
            public CrosswordInvalidCellsAssignedToRow(int row) : base("Row " + row.ToString() + " rejected assigned cells.") {}
        }
        private class CrosswordInvalidRowNumberAssignedToRow : Exception
        { 
            public CrosswordInvalidRowNumberAssignedToRow(int row) : base("Row rejected " + row.ToString() + " as row number because it is not an allowed value.") {}
        }
        private class CrosswordInvalidColumnNumberAssignedToColumn : Exception
        { 
            public CrosswordInvalidColumnNumberAssignedToColumn(int col) : base("Column rejected " + col.ToString() + " as column number because it is not an allowed value.") {}
        }
        private class CrosswordLastWordCheckFail : Exception
        { 
            public CrosswordLastWordCheckFail(string word) : base("The generated crossword has some problem.\nThe word " + word + " at inserted is not in the _dictionary. Logic error!") {}
        }
        
        private class Cell
        {
            public readonly int _column;
            public readonly int _row;
            private char _val;

            public Cell(int column, int row)
            {
                if (column<0) throw new CrosswordInvalidColumnAssignedToCell(column);
                if (row<0) throw new CrosswordInvalidRowAssignedToCell(row);

                _column = column;
                _row = row;
                _val = EMPTY_CELL;
            }
            
            public char Val {
                get => _val;
                set
                {
                    if (false) throw new CrosswordInvalidValAssignedToCell(value);
                    _val = value;
                }
            }

            public override string ToString() => _val.ToString(); 
        }

        private class Word
        {
            public readonly List<Cell> _cells;
            public readonly Cell _first_cell;
            public readonly int _idx;
            public readonly int _len;
            private readonly bool _horizontal;
            public List<string> _alternative_words = new();

            public Word() {}

            public Word(int idx, List<Cell> cells)
            {
                if (cells.Count<=1) throw new CrosswordInvalidCellsAssignedToWord();
                _cells = cells;
                _first_cell = _cells.First();
                _len = cells.Count;
                _idx = idx;
                _horizontal = (cells[0]._row == cells[1]._row);
            }

            public string word => string.Join("",(from cell in _cells select cell.Val).ToArray());

            //public string word => (from cell in _cells select cell.Val).ToArray().ToString();

            public int Word_Index { get => _idx;}

            public void Set_Alternative_Words(List<string> list)
            {
                if (_alternative_words.Count>0) throw new CrosswordTryingToOverwriteAlternativeWords();
                    _alternative_words = new List<string>(list);
            }

            public void Reset_All()
            {
                foreach (var cell in _cells) cell.Val = EMPTY_CELL;
                Reset_Alternative_Words();
            }

            public void Reset_Alternative_Words() => _alternative_words = new List<string>();
            
            public override string ToString()
            {
                string tmp_str = "";
                foreach (var cell in _cells) tmp_str += cell.ToString(); 
                return tmp_str;
            }

            public void Set_Word(string new_word)
            {
                foreach (var tuple in _cells.Zip(new_word)) tuple.First.Val = tuple.Second;
            }

            public bool Use_Another_Alternative_Word(bool sequentially=false)
            {
                if (_alternative_words.Count>0)
                {   
                    string tmp_word;
                    if (!sequentially)
                    {
                        tmp_word = _alternative_words.ElementAt(_rand.Next(0,_alternative_words.Count-1));
                        _alternative_words.Remove(tmp_word);
                    }
                    else 
                    {
                        tmp_word = _alternative_words[0];
                        _alternative_words.RemoveAt(0);
                    }
                    Set_Word(tmp_word);
                    
                    return true;
                }
                else return false;
            } 

            public bool Is_Compatible_With(char[] test_word) => (_len == test_word.Length) && !this.word.Zip(test_word).Any(tuple => tuple.First != tuple.Second && tuple.First != EMPTY_CELL && tuple.Second != EMPTY_CELL);
            
            public List<char> Get_All_Possible_Letters(Cell cell, List<string> Dictionary)
            {   
                //if (!_cells.Any(tmp_cell => (tmp_cell._column,tmp_cell._row) == (cell._column,cell._row))) throw new CrosswordInvalidCellTestedOnWord(cell,this); 

                int cell_idx = _horizontal ? cell._column - _first_cell._column : cell._row - _first_cell._row;

                return new List<char>(Dictionary
                                        .Where(word => word.Length == _len && this.Is_Compatible_With(word.ToCharArray()))
                                        .Select(word => word[cell_idx])
                                        .Distinct());
            }
        }

        private class Row
        {
            public readonly List<Cell> _cells;
            public readonly int _column_max;
            public readonly int _row;
            public readonly List<Word> _words = new();

            public Row(int row, List<Cell> cells)
            {
                if (row<0) throw new CrosswordInvalidRowNumberAssignedToRow(row);
                if (cells.Count<=0) throw new CrosswordInvalidCellsAssignedToRow(row);
                _cells = cells;
                _column_max = cells.Count-1;
                _row = row;
            }

            public void Init_Words()
            {
                _words.Clear();
                int tmp_column = -1;
                int tmp_len = 0;
                int idx = 0;
                foreach (var cell in _cells)
                {
                    if (cell.Val != BLACK_CELL)
                    {
                        if (tmp_column == -1) tmp_column = cell._column;
                        tmp_len += 1;
                    }

                    if (tmp_len > 1 && ((cell.Val != BLACK_CELL && cell._column == _column_max) || cell.Val == BLACK_CELL))
                    {
                        idx = _words.Count;
                        _words.Add(new Word( idx, _cells
                                                    .Where(tmp_cell => Enumerable.Range(tmp_column,tmp_len).Contains(tmp_cell._column)) 
                                                    .OrderBy(tmp_cell => tmp_cell._column)
                                                    .ToList())); 
                    }
                    if (cell.Val == BLACK_CELL)
                    {
                        tmp_column = -1;
                        tmp_len = 0;
                    }
                }
                
                
            }
            
            public override string ToString()
            {
                string tmp_str = _row.ToString();
                foreach (var cell in _cells) tmp_str += "  " + cell.ToString();
                return tmp_str;
            }
        
            public Word Get_Word_At_Column(int column, out bool NotFound)
            {   
                NotFound = false;
                foreach (var word in _words)
                    foreach (var cell in word._cells)
                        if (cell._column == column) return word;

                NotFound = true;
                return _words[0];
            }
        }

        private class Column
        {
            public readonly List<Cell> _cells;
            public readonly int _row_max;
            public readonly int _column;
            public readonly List<Word> _words = new();

            public Column(int column,List<Cell> cells)
            {
                if (column<0) throw new CrosswordInvalidColumnNumberAssignedToColumn(column);
                if (cells.Count<=0) throw new CrosswordInvalidCellsAssignedToColumn(column);
                _cells = cells;
                _row_max = cells.Count-1;
                _column = column;
            }

            public void Init_Words()
            {
                _words.Clear();
                int tmp_row = -1;
                int tmp_len = 0;
                int idx = 0;

                foreach (var cell in _cells)
                {
                    if (cell.Val != BLACK_CELL)
                    {
                        if (tmp_row == -1) tmp_row = cell._row;
                        tmp_len += 1;
                    }

                    if (tmp_len > 1 && ((cell.Val != BLACK_CELL && cell._row == _row_max) || cell.Val == BLACK_CELL))
                    {
                        idx = _words.Count;
                        _words.Add(new Word(idx, _cells
                                                    .Where(tmp_cell => Enumerable.Range(tmp_row,tmp_len).Contains(tmp_cell._row)) 
                                                    .OrderBy(tmp_cell => tmp_cell._row)
                                                    .ToList()));
                    }
                    if (cell.Val == BLACK_CELL)
                    {
                        tmp_row = -1;
                        tmp_len = 0;
                    }
                }
            }
            
            public override string ToString()
            {
                string tmp_str = "";
                foreach (var cell in _cells) tmp_str = tmp_str + cell.ToString();
                return tmp_str;
            }
    
            public Word Get_Word_At_Row(int row, out bool NotFound)
            {
                NotFound = false;
                try
                {
                    return (from word in _words select word).Where(word => word._cells.Any(cell => cell._row == row)).First();
                }
                catch
                {
                    NotFound = true;
                    return new Word();
                }
            }
        }

        public Crossword(int column_num, int row_num, string user_word, string dictionary_file)
        {
            if (column_num < MIN_COLUMN_VALUE || column_num > MAX_COLUMN_VALUE) throw new CrosswordInvalidNumberOfColumns(column_num);
            if (row_num < MIN_ROW_VALUE || row_num > MAX_ROW_VALUE) throw new CrosswordInvalidNumberOfRows(row_num);
            if (!File.Exists(dictionary_file) || Path.GetExtension(dictionary_file)!=".txt") throw new CrosswordInvalidDictionaryFile(dictionary_file);
            if (user_word.Length > column_num || user_word.ToUpper().Any(letter => !_alphabet.Contains(letter))) throw new CrosswordInvalidUserWord(user_word);

            _column_num = column_num;
            _row_num = row_num;
            _dictionary_file = dictionary_file;
            _user_word = user_word;
            _timeout = 3;

            //Create cells
            foreach (var row in Enumerable.Range(0,_row_num)) 
                foreach (var column in Enumerable.Range(0,_column_num)) 
                    _cells.Add(new Cell(column,row));
            

            //Create rows and link it to cells
            foreach (var row in Enumerable.Range(0,_row_num)) 
                _rows.Add(new Row(row, _cells
                                        .Where(cell => cell._row == row)
                                        .OrderBy(cell => cell._column)
                                        .ToList()));

            //Create columns and link it to cells
            foreach (var column in Enumerable.Range(0,_column_num)) 
                _columns.Add(new Column(column, _cells
                                                    .Where(cell => cell._column == column)
                                                    .OrderBy(cell => cell._row)
                                                    .ToList()));
        }

        private List<string> GetAllCompatibleWords(List<List<char>> cross_letters)
        {
            return _dictionary
                        .AsParallel()
                        .Where(word => word.Length == cross_letters.Count)
                        .Where(word => (from idx in Enumerable.Range(0,word.Length) select idx).All(idx => cross_letters[idx].Contains(word[idx])))
                        .ToList();
        }

        private bool SearchWordOnRow(Row row, Word word)
        {
            List<List<char>> tmp_crossletters = new();
            List<string> tmp_words = new();

            if (word._cells.Any(cell => cell.Val == EMPTY_CELL))
            {   
                {//Search tmp_crossletters
                    tmp_crossletters.Clear();

                    bool vertical_word_nok;
                    
                    foreach (var cell in word._cells) {
                        Word vertical_word;
                        vertical_word = _columns[cell._column].Get_Word_At_Row(row._row,out vertical_word_nok);
                        if (!vertical_word_nok)
                            tmp_crossletters.Add(vertical_word.Get_All_Possible_Letters(cell,_dictionary));
                        else{
                            tmp_crossletters.Add(_alphabet);
                        }
                    }
                }
                
                {//Search possible words based on tmp_crossletters
                    tmp_words = GetAllCompatibleWords(tmp_crossletters);
                    // Console.WriteLine("Found words:");
                    // foreach (var tmp_word in tmp_words)
                    //     Console.WriteLine(tmp_word);
                }

                {//Assign found word to actual word
                    if (word._alternative_words.Count == 0) //This is the first time
                    {
                        if (tmp_words.Count > 0)
                        {
                            word.Set_Alternative_Words(tmp_words);

                            word.Use_Another_Alternative_Word();
                        }
                        else
                        {
                            //failure branch -> i have to go back to the previous branch in order to try another way to get the solution
                            //print('\nfail at row ' + str(row.r) + ' for word ' + str(w_idx+1))
                            _failing_r_idx = row._row;
                            _failing_w_idx = word._idx;
                            return false;

                            // Console.WriteLine("fail");
                            // Console.ReadLine();
                        }
                    }
                    else
                        word.Use_Another_Alternative_Word();
                }

            }
            return true;
        }

        private bool SearchWordOnColumn(Column column, Word word)
        {
            List<List<char>> tmp_crossletters = new();
            List<string> tmp_words = new();

            if (word._cells.Any(cell => cell.Val == EMPTY_CELL))
            {   
                word.Set_Word(_dictionary
                                .Where(tmp_word => word.Is_Compatible_With(tmp_word.ToCharArray()))
                                .First());
                
            }
            return true;
        }

        public bool Generate()
        {
            int branchchanged_r_idx = 0;
            int branchchanged_w_idx = 0;
            bool branch_changed = false;
            bool fail_flag = false;
            _generate_start_dt = DateTime.Now;

            InitDictionary();
            GenerateBasicSchema();

            { //Rows...
                while(true)
                {
                    foreach (var row in _rows)
                    {   
                        if((DateTime.Now - _generate_start_dt).TotalSeconds>_timeout)
                        {
                            this.Generate();
                            return true;
                        }
                        Console.Clear();
                        Console.Write(this.ToString());
                        List<Task<bool>> tasks = new List<Task<bool>>();
                        foreach (var word in row._words)
                        {
                            if (word.word.Any(letter => letter == EMPTY_CELL))
                            {
                                var task = new Task<bool>(()=> SearchWordOnRow(row,word));
                                task.Start();
                                tasks.Add(task);
                            }
                        }
                        Task.WaitAll(tasks.ToArray());
                        fail_flag = tasks.Any(task => task.Result == false);
                        if (fail_flag) break;
                    }
                    if (fail_flag)
                    {
                        branch_changed = false;
                        //go back to the last branch resetting all words without alternatives. Restore the alternative in the branch found.
                        for (int r = _failing_r_idx; r >= 0 ; r--)
                        {
                            for (int w = _rows[r]._words.Count-1; w >= 0 ; w--)
                            {
                                if (r == _failing_r_idx && w == _failing_w_idx) //this is the failing word
                                    continue;
                                if (_user_word.Length > 0){
                                    if ((_rows[r]._words[w]._first_cell._column,_rows[r]._words[w]._first_cell._row)==(_user_word_cell._column,_user_word_cell._row)) //this is the user word
                                        continue;
                                }
                                if (this.AreWordsColumnDependent(_rows[r]._words[w],_rows[_failing_r_idx]._words[_failing_w_idx]))
                                {
                                    if (_rows[r]._words[w].Use_Another_Alternative_Word())
                                    {
                                        branchchanged_r_idx = r;
                                        branchchanged_w_idx = w;
                                        branch_changed = true;
                                        break;
                                    }
                                    else _rows[r]._words[w].Reset_All();
                                }
                                if (branch_changed) break;
                            }
                        }

                        if (!branch_changed)
                        {
                            //Console.WriteLine("FAIL");
                            //try again with a different schema...
                            return this.Generate();
                        }
                        else
                        {
                            //branch changed -> reset all dependent words after this
                            for (int r = branchchanged_r_idx; r < _rows.Count; r++)
                            {
                                for (int w = 0; w < _rows[r]._words.Count; w++)
                                {
                                    if (r == branchchanged_r_idx && w <= branchchanged_w_idx) //this is new branch word
                                        continue;
                                    if (_user_word.Length > 0){
                                        if ((_rows[r]._words[w]._first_cell._column,_rows[r]._words[w]._first_cell._row) == (_user_word_cell._column,_user_word_cell._row)) //this is the user word
                                            continue;
                                    }
                                    if (this.AreWordsColumnDependent(_rows[r]._words[w],_rows[branchchanged_r_idx]._words[branchchanged_w_idx]))
                                        _rows[r]._words[w].Reset_All();
                                }
                            }
                        }

                    }
                    else break; //no erro -> crossword is completed
                }
            }
            { //Columns...
                foreach (var column in _columns)
                    {
                        List<Task<bool>> tasks = new List<Task<bool>>();
                        foreach (var word in column._words)
                        {
                            if (word.word.Any(letter => letter == EMPTY_CELL))
                            {
                                var task = new Task<bool>(()=> SearchWordOnColumn(column,word));
                                task.Start();
                                tasks.Add(task);
                            }
                            Task.WaitAll(tasks.ToArray());
                            fail_flag = tasks.Any(task => task.Result == false);
                        }
                    }
            }
            { //Fill remaining cells...
                foreach (var cell in _cells)
                    if (cell.Val == EMPTY_CELL) cell.Val = BLACK_CELL;
            }
            { //Check all words
                foreach (var row in _rows)
                    foreach (var word in row._words)
                        if (!_dictionary.Contains(word.word) && word.word!=_user_word) throw new CrosswordLastWordCheckFail(word.word);

                foreach (var column in _columns)
                    foreach (var word in column._words)
                        if (!_dictionary.Contains(word.word) && word.word!=_user_word) throw new CrosswordLastWordCheckFail(word.word);
                    
            }
            
            return true;
        }

        private void GenerateBasicSchema()
        {
            //Reset all cells
            foreach (var cell in _cells) cell.Val = EMPTY_CELL;            
            
            if (true)
            {
                //Inser _user_word 
                if (_user_word.Length > 0)
                {
                    int user_word_row = _rand.Next(0,2);
                    int user_word_column = _rand.Next(0,_column_num -_user_word.Length);
                    _user_word_cell = _cells.First(cell => cell._row == user_word_row && cell._column == user_word_column);
                    foreach (var cell in _rows[user_word_row]._cells)
                    {
                        if (cell._column == user_word_column-1) cell.Val = BLACK_CELL; //Insert black cell before the _user_word
                        if (cell._column == user_word_column+_user_word.Length) cell.Val = BLACK_CELL; //Insert black cell after the _user_word
                        if (Enumerable.Range(user_word_column,_user_word.Length).Contains(cell._column)) cell.Val = _user_word[cell._column - user_word_column]; //Insert _user_word letters
                    }
                }
                else
                {

                }

                //Add random black cells
                foreach (var cell in _cells) if (_rand.Next(0,100) < BLACK_CELL_PERCENTAGE && cell.Val == EMPTY_CELL) cell.Val = BLACK_CELL;
                
                //Check max word len on rows
                int tmp_len;
                int tmp_column;
                foreach (var row in _rows)
                {
                    tmp_column = -1;
                    tmp_len = 0;
                    foreach (var cell in row._cells)
                    {
                        if (cell.Val == EMPTY_CELL)
                        {
                            if(tmp_column<0) tmp_column = cell._column;
                            tmp_len += 1;
                        }
                        if (tmp_len>MAX_WORD_LEN)
                        {
                            row._cells
                                    .Where(cell => cell.Val == EMPTY_CELL)
                                    .Last(cell => Enumerable.Range(tmp_column,tmp_len).Contains(cell._column))
                                    .Val = BLACK_CELL;
                            tmp_len = 0;
                        }
                        if (cell.Val != EMPTY_CELL) 
                        {
                            tmp_column = -1;
                            tmp_len = 0;
                        }
                    }
                }

                //Check max word len on columns
                int tmp_row;
                foreach (var column in _columns)
            {
                tmp_row = -1;
                tmp_len = 0;
                foreach (var cell in column._cells)
                {
                    if (cell.Val == EMPTY_CELL)
                    {
                        if(tmp_row<0) tmp_row = cell._row;
                        tmp_len += 1;
                    }
                    if (tmp_len>MAX_WORD_LEN)
                    {
                        column._cells
                                    .Where(cell => cell.Val == EMPTY_CELL)
                                    .Last(cell => Enumerable.Range(tmp_row,tmp_len).Contains(cell._row))
                                    .Val = BLACK_CELL;
                        tmp_len = 0;
                    }
                    if (cell.Val != EMPTY_CELL) 
                    {
                        tmp_row = -1;
                        tmp_len = 0;
                    }
                }
            }
            }
            else
            {
                _user_word = "MIRACOLI";
                _user_word_cell = _rows[0]._cells[0];
                _rows[0]._cells[0].Val = 'M';
                _rows[0]._cells[1].Val = 'I';
                _rows[0]._cells[2].Val = 'R';
                _rows[0]._cells[3].Val = 'A';
                _rows[0]._cells[4].Val = 'C';
                _rows[0]._cells[5].Val = 'O';
                _rows[0]._cells[6].Val = 'L';
                _rows[0]._cells[7].Val = 'I';
                _rows[0]._cells[8].Val = BLACK_CELL;

                _rows[1]._cells[4].Val = BLACK_CELL;
                
                _rows[2]._cells[3].Val = BLACK_CELL;
                _rows[2]._cells[5].Val = BLACK_CELL;
                _rows[2]._cells[12].Val = BLACK_CELL;
                
                _rows[3]._cells[2].Val = BLACK_CELL;
                _rows[3]._cells[6].Val = BLACK_CELL;
                _rows[3]._cells[10].Val = BLACK_CELL;
                
                _rows[4]._cells[1].Val = BLACK_CELL;
                _rows[4]._cells[7].Val = BLACK_CELL;
                _rows[4]._cells[9].Val = BLACK_CELL;
                
                _rows[5]._cells[1].Val = BLACK_CELL;
                _rows[5]._cells[8].Val = BLACK_CELL;
                
                _rows[6]._cells[2].Val = BLACK_CELL;
                _rows[6]._cells[7].Val = BLACK_CELL;
                _rows[6]._cells[9].Val = BLACK_CELL;
            
                _rows[7]._cells[0].Val = BLACK_CELL;
                _rows[7]._cells[4].Val = BLACK_CELL;
                _rows[7]._cells[6].Val = BLACK_CELL;
                _rows[7]._cells[10].Val = BLACK_CELL;
                
                _rows[8]._cells[5].Val = BLACK_CELL;
                _rows[8]._cells[11].Val = BLACK_CELL;
                
                _rows[9]._cells[2].Val = BLACK_CELL;
                _rows[9]._cells[4].Val = BLACK_CELL;
                _rows[9]._cells[12].Val = BLACK_CELL;
                
                _rows[10]._cells[7].Val = BLACK_CELL;
                _rows[10]._cells[9].Val = BLACK_CELL;
                
                _rows[11]._cells[3].Val = BLACK_CELL;
                _rows[11]._cells[8].Val = BLACK_CELL;
                
                _rows[12]._cells[0].Val = BLACK_CELL;
                _rows[12]._cells[4].Val = BLACK_CELL;
                _rows[12]._cells[10].Val = BLACK_CELL;
            }
            //Init words on rows
            foreach (var row in _rows) row.Init_Words();

            //Init words on columns
            foreach (var column in _columns) column.Init_Words();
        }
        public override string ToString()
        {
            string tmp_str = "\n   ";
            for (int column = 0; column < _column_num; column++) 
            {
                if(column<10) tmp_str += " ";
                tmp_str += column.ToString() + " ";
            }
            tmp_str += "\n";
            
            foreach (var row in _rows) 
            {
                if(_rows.IndexOf(row)<10) tmp_str += " ";
                tmp_str += row.ToString() + '\n';
            }
            tmp_str += "\nElaborating time:" + (DateTime.Now - _generate_start_dt).TotalSeconds + "s";
            
            return tmp_str;
        }

        private bool InitDictionary()
        {
            if (!File.Exists(_dictionary_file) || Path.GetExtension(_dictionary_file)!=".txt") throw new CrosswordInvalidDictionaryFile(_dictionary_file);

            _dictionary = File
                            .ReadAllLines(_dictionary_file)
                            .Select(word => word.ToUpper())
                            .Where(word => word.Length <= MAX_WORD_LEN && word.Length>1 && word.All(letter => _alphabet.Contains(letter)))
                            .Union(from letter1 in _alphabet from letter2 in _alphabet select letter1.ToString()+letter2.ToString())
                            .Distinct()
                            .OrderBy(word => word.Length)
                            .ToList();            
            
            return true;
        }
    
        private bool AreWordsColumnDependent(Word word1,Word word2)
        {
            bool NotFound = false;

            var common_columns_idx = (from cell1 in word1._cells select cell1._column)
                                    .Intersect(from cell2 in word2._cells select cell2._column);

            foreach (var column_idx in common_columns_idx)
            {
                var vertical_word1 = _columns[column_idx].Get_Word_At_Row(word1._first_cell._row, out NotFound); 
                if (NotFound) continue;
                var vertical_word2 = _columns[column_idx].Get_Word_At_Row(word2._first_cell._row, out NotFound); 
                if (NotFound) continue;

                if (_columns[column_idx].Get_Word_At_Row(word1._first_cell._row, out NotFound)._first_cell._row == _columns[column_idx].Get_Word_At_Row(word2._first_cell._row, out NotFound)._first_cell._row)
                    return true;  
            }
            return false;
        }
    }

    class Program
    {   
                
        static void Main(string[] argv)
        {   
            Crossword crossword = new Crossword(15,15,"FANCULO","C:\\Data\\CrosswordGeneratorCS\\parole.txt");
            
            crossword.Generate();

            Console.Clear();
            Console.WriteLine(crossword.ToString());
            
        }
    }
}

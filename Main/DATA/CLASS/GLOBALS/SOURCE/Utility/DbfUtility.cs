using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Forms;

namespace Globals.SOURCE.Utility
{
    public struct DBFColumnType
    {
        private string strColumnName;
        private Type tColumnType;
        private ushort intPrecision;
        private ushort intScale;

        public string ColumnName { get { return this.strColumnName; } set { this.strColumnName = value; } }
        public Type ColumnType { get { return this.tColumnType; } set { this.tColumnType = value; } }
        public ushort Precision { get { return this.intPrecision; } set { this.intPrecision = value; } }
        public ushort Scale { get { return this.intScale; } set { this.intScale = value; } }
    }

    internal class DBFColumnsList
    {
        private List<DBFColumnType> lstColumns;
        public DBFColumnsList() { this.lstColumns = new List<DBFColumnType>(); }
        public int Count { get { return this.lstColumns.Count; } set { } }
        public DBFColumnType this[int Index] { get { return this.lstColumns[Index]; } set { this.lstColumns[Index] = value; } }
        public void Remove(DBFColumnType Column) { this.lstColumns.Remove(Column); }
        public void Clear() { this.lstColumns.Clear(); }
        public void Add(string ColumnName, Type ColumnType, ushort Precision)
        {
            DBFColumnType dctColumn = new DBFColumnType();
            dctColumn.ColumnName = ColumnName;
            dctColumn.ColumnType = ColumnType;
            dctColumn.Precision = Precision;
            dctColumn.Scale = 5;
            this.lstColumns.Add(dctColumn);
        }
        public void Add(string ColumnName, Type ColumnType, ushort Precision, ushort Scale)
        {
            DBFColumnType dctColumn = new DBFColumnType();
            dctColumn.ColumnName = ColumnName;
            dctColumn.ColumnType = ColumnType;
            dctColumn.Precision = Precision;
            dctColumn.Scale = Scale;
            this.lstColumns.Add(dctColumn);
        }
    }

    public class exDBF
    {
        private DBFColumnsList dclColumns;
        private DataTable dtTable;
        private string strTableName;

        public exDBF(string TableName)
        {
            dclColumns = new DBFColumnsList();
            dtTable = new DataTable(TableName);
            this.strTableName = TableName;
        }

        public DataTable DataTable { get { return this.dtTable; } set { this.DataTable = value; } }

        public void AddColumn(string ColumnName, Type ColumnType, ushort Precision, ushort Scale)
        {
            dclColumns.Add(ColumnName, ColumnType, Precision, Scale);
            dtTable.Columns.Add(ColumnName, ColumnType);
        }


        /// <summary>
        /// Загружает в DBF таблицу
        /// </summary>
        /// <param name="dataTable"></param>
        public void AddTable(DataTable dataTable)
        {
            foreach (DataColumn dc in dataTable.Columns)
            {
                AddColumn(dc.ColumnName, dc.DataType, 0, 0);
            }
            foreach (DataRow dr in dataTable.Rows)
            {
                DataTable.ImportRow(dr);
            }
        }

        /// <summary>
        /// Save DataTable into *.DBF file
        /// </summary>
        /// <param name="DT">DataTable object</param>
        /// <param name="Columns">DBFColumnsList object</param>
        /// <param name="Folder">Path of folder to save. File name is [DataTable.TableName].DBF</param>
        /// <param name="Encoding">Use 866 for Russian DOS or 1251 for Russian Windows</param>
        public void Save(string Folder, int Encoding)
        {
            // Создаю таблицу
            System.IO.File.Delete(Folder + "\\" + dtTable.TableName + ".DBF");
            System.IO.FileStream FS = new System.IO.FileStream(Folder + "\\" + dtTable.TableName + ".DBF", System.IO.FileMode.Create);
            // Формат dBASE III 2.0
            byte[] buffer = new byte[] { 0x03, 0x63, 0x04, 0x04 }; // Заголовок  4 байта
            FS.Write(buffer, 0, buffer.Length);
            buffer = new byte[]{
                       (byte)(((dtTable.Rows.Count % 0x1000000) % 0x10000) % 0x100),
                       (byte)(((dtTable.Rows.Count % 0x1000000) % 0x10000) / 0x100),
                       (byte)(( dtTable.Rows.Count % 0x1000000) / 0x10000),
                       (byte)(  dtTable.Rows.Count / 0x1000000)
                      }; // Word32 -> кол-во строк 5-8 байты
            FS.Write(buffer, 0, buffer.Length);
            int i = (dtTable.Columns.Count + 1) * 32 + 1; // Изврат
            buffer = new byte[]{
                       (byte)( i % 0x100),
                       (byte)( i / 0x100)
                      }; // Word16 -> кол-во колонок с извратом 9-10 байты
            FS.Write(buffer, 0, buffer.Length);
            string[] FieldName = new string[dtTable.Columns.Count]; // Массив названий полей
            string[] FieldType = new string[dtTable.Columns.Count]; // Массив типов полей
            byte[] FieldSize = new byte[dtTable.Columns.Count]; // Массив размеров полей
            byte[] FieldDigs = new byte[dtTable.Columns.Count]; // Массив размеров дробной части
            int s = 1; // Считаю длину заголовка
            int nc = 0;
            foreach (DataColumn C in dtTable.Columns)
            {
                ++nc;
                string l = C.ColumnName.ToUpper(); // Имя колонки
                if (l.Length >= 10)
                {
                    l.Substring(0, (9 - ((int) Math.Log10(nc))));
                    l += nc;
                }
                while (l.Length < 10) { l = l + (char)0; } // Подгоняю по размеру (10 байт)
                FieldName[C.Ordinal] = l.Substring(0, 10) + (char)0; // Результат
                FieldType[C.Ordinal] = "C";
                FieldSize[C.Ordinal] = 50;
                FieldDigs[C.Ordinal] = 0;

                switch (C.DataType.ToString())
                {
                    case "System.String":
                        {
                            if (dclColumns[C.Ordinal].ColumnName == C.ColumnName)
                            {
                                int n = dclColumns[C.Ordinal].Precision;
                                if (n > 255 || n < 1) FieldSize[C.Ordinal] = 255;
                                else FieldSize[C.Ordinal] = (byte)n;
                            }
                            else FieldSize[C.Ordinal] = 255;
                            break;
                        }
                    case "System.Double":
                        {
                            if (FieldName[C.Ordinal].Replace("\0", "") == dclColumns[C.Ordinal].ColumnName.ToUpper())
                            {
                                FieldType[C.Ordinal] = "F";
                                if (dclColumns[C.Ordinal].Precision > 0) FieldSize[C.Ordinal] = (byte)dclColumns[C.Ordinal].Precision;
                                else FieldSize[C.Ordinal] = 38;
                                FieldDigs[C.Ordinal] = (byte)dclColumns[C.Ordinal].Scale;
                            }
                            else
                            {
                                FieldType[C.Ordinal] = "F";
                                FieldSize[C.Ordinal] = 38;
                                FieldDigs[C.Ordinal] = 5;
                            }
                            break;
                        }
                    case "System.Single":
                        {
                            if (FieldName[C.Ordinal].Replace("\0", "") == dclColumns[C.Ordinal].ColumnName.ToUpper())
                            {
                                FieldType[C.Ordinal] = "F";
                                if (dclColumns[C.Ordinal].Precision > 0) FieldSize[C.Ordinal] = (byte)dclColumns[C.Ordinal].Precision;
                                else FieldSize[C.Ordinal] = 38;
                                FieldDigs[C.Ordinal] = (byte)dclColumns[C.Ordinal].Scale;
                            }
                            else
                            {
                                FieldType[C.Ordinal] = "F";
                                FieldSize[C.Ordinal] = 38;
                                FieldDigs[C.Ordinal] = 5;
                            }
                            break;
                        }
                    case "System.Decimal":
                        {
                            if (FieldName[C.Ordinal].Replace("\0", "") == dclColumns[C.Ordinal].ColumnName.ToUpper())
                            {
                                FieldType[C.Ordinal] = "N";
                                if (dclColumns[C.Ordinal].Precision > 0) FieldSize[C.Ordinal] = (byte)dclColumns[C.Ordinal].Precision;
                                else FieldSize[C.Ordinal] = 38;
                                FieldDigs[C.Ordinal] = (byte)dclColumns[C.Ordinal].Scale;
                            }
                            else
                            {
                                FieldType[C.Ordinal] = "N";
                                FieldSize[C.Ordinal] = 38;
                                FieldDigs[C.Ordinal] = 5;
                            }
                            break;
                        }
                    case "System.Byte": { FieldType[C.Ordinal] = "N"; FieldSize[C.Ordinal] = 1; break; }
                    case "System.Int16": { FieldType[C.Ordinal] = "N"; FieldSize[C.Ordinal] = 6; break; }
                    case "System.Int32": { FieldType[C.Ordinal] = "N"; FieldSize[C.Ordinal] = 11; break; }
                    case "System.Int64": { FieldType[C.Ordinal] = "N"; FieldSize[C.Ordinal] = 21; break; }
                    case "System.SByte": { FieldType[C.Ordinal] = "N"; FieldSize[C.Ordinal] = 6; break; }
                    case "System.UInt16": { FieldType[C.Ordinal] = "N"; FieldSize[C.Ordinal] = 6; break; }
                    case "System.UInt32": { FieldType[C.Ordinal] = "N"; FieldSize[C.Ordinal] = 11; break; }
                    case "System.UInt64": { FieldType[C.Ordinal] = "N"; FieldSize[C.Ordinal] = 21; break; }
                    case "System.Boolean": { FieldType[C.Ordinal] = "L"; FieldSize[C.Ordinal] = 1; break; }
                    case "System.DateTime": { FieldType[C.Ordinal] = "D"; FieldSize[C.Ordinal] = 8; break; }
                }
                s = s + FieldSize[C.Ordinal];
            }
            buffer = new byte[]{
                       (byte)(s % 0x100), 
                       (byte)(s / 0x100)
                      }; // Пишу длину заголовка 11-12 байты
            FS.Write(buffer, 0, buffer.Length);
            for (int j = 0; j < 17; j++) { FS.WriteByte(0x00); } // Пишу всякий хлам
            byte byteEncoding = 0x26;
            switch (Encoding)
            {
                case 866:
                    byteEncoding = 0x26;
                    break;
                case 1251:
                    byteEncoding = 0xC9;
                    break;
                default: break;
            }
            FS.WriteByte(byteEncoding); // Кодировка, 29-й байт
            for (int j = 0; j < 2; j++) { FS.WriteByte(0x00); } // Пишу всякий хлам
            //  итого: 32 байта - базовый заголовок DBF
            // Заполняю заголовок
            foreach (DataColumn C in dtTable.Columns)
            {
                buffer = System.Text.Encoding.ASCII.GetBytes(FieldName[C.Ordinal]); // Название поля
                FS.Write(buffer, 0, buffer.Length);
                buffer = new byte[]{
                        System.Text.Encoding.ASCII.GetBytes(FieldType[C.Ordinal])[0],
                        0x00, 
                        0x00,
                        0x00, 
                        0x00
                       }; // Размер
                FS.Write(buffer, 0, buffer.Length);
                buffer = new byte[]{
                        FieldSize[C.Ordinal],
                        FieldDigs[C.Ordinal]
                       }; // Размерность
                FS.Write(buffer, 0, buffer.Length);
                buffer = new byte[]{
                        0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00
                }; // 14 нулей
                FS.Write(buffer, 0, buffer.Length);
            }
            FS.WriteByte(0x0D); // Конец описания таблицы
            System.Globalization.DateTimeFormatInfo dfi = new System.Globalization.CultureInfo("en-US", false).DateTimeFormat;
            System.Globalization.NumberFormatInfo nfi = new System.Globalization.CultureInfo("en-US", false).NumberFormat;
            string Spaces = "";
            while (Spaces.Length < 255) Spaces = Spaces + " ";
            foreach (DataRow R in dtTable.Rows)
            {
                FS.WriteByte(0x20); // Пишу данные
                foreach (DataColumn C in dtTable.Columns)
                {
                    string l = R[C].ToString();
                    if (l != "") // Проверка на NULL
                    {
                        switch (FieldType[C.Ordinal])
                        {
                            case "L":
                                {
                                    l = bool.Parse(l).ToString();
                                    break;
                                }
                            case "N":
                                {
                                    l = decimal.Parse(l).ToString(nfi);
                                    break;
                                }
                            case "F":
                                {
                                    l = double.Parse(l).ToString(nfi);
                                    break;
                                }
                            case "D":
                                {
                                    l = DateTime.Parse(l).ToString("yyyyMMdd", dfi);
                                    break;
                                }
                            default: l = l.Trim() + Spaces; break;
                        }
                    }
                    else
                    {
                        if (FieldType[C.Ordinal] == "C"
                         || FieldType[C.Ordinal] == "D")
                            l = Spaces;
                    }
                    while (l.Length < FieldSize[C.Ordinal]) { l += (char)0x00; }
                    l = l.Substring(0, FieldSize[C.Ordinal]); // Корректирую размер
                    buffer = System.Text.Encoding.GetEncoding(Encoding).GetBytes(l); // Записываю в кодировке (MS-DOS Russian)
                    FS.Write(buffer, 0, buffer.Length);
                    Application.DoEvents();
                }
            }
            FS.WriteByte(0x1A); // Конец данных
            FS.Close();
        }

        /// <summary>
        /// Load data into DataTable from *.DBF file
        /// Automatically identify encoding [Russian DOS or Russian Windows]
        /// </summary>
        /// <param name="FileName">Path to *.DBF file</param>
        /// <returns>DataTable object</returns>
        public DataTable Load(string FileName, int FileEncoding = 0)
        {
            this.dtTable.Clear();
            this.dclColumns.Clear();
            this.dtTable.TableName = strTableName;
            System.IO.FileStream FS = new System.IO.FileStream(FileName, System.IO.FileMode.Open);
            byte[] buffer = new byte[4]; // Кол-во записей: 4 байтa, начиная с 5-го
            FS.Position = 4; FS.Read(buffer, 0, buffer.Length);
            int RowsCount = buffer[0] + (buffer[1] * 0x100) + (buffer[2] * 0x10000) + (buffer[3] * 0x1000000);
            buffer = new byte[2]; // Кол-во полей: 2 байтa, начиная с 9-го
            FS.Position = 8; FS.Read(buffer, 0, buffer.Length);
            int FieldCount = (((buffer[0] + (buffer[1] * 0x100)) - 1) / 32) - 1;
            int Encoding;
            if (FileEncoding == 0)
            {
                FS.Position = 29;
                Encoding = FS.ReadByte(); // Кодировка
                switch (Encoding)
                {
                    case 0x26:
                        Encoding = 866;
                        break;
                    case 0xC9:
                        Encoding = 1251;
                        break;
                    default:
                        break;
                }
            }
            else Encoding = FileEncoding;
            string[] FieldName = new string[FieldCount]; // Массив названий полей
            string[] FieldType = new string[FieldCount]; // Массив типов полей
            byte[] FieldSize = new byte[FieldCount]; // Массив размеров полей
            byte[] FieldDigs = new byte[FieldCount]; // Массив размеров дробной части
            buffer = new byte[32 * FieldCount]; // Описание полей: 32 байта * кол-во, начиная с 33-го
            FS.Position = 32; FS.Read(buffer, 0, buffer.Length);
            int FieldsLength = 0;
            for (int i = 0; i < FieldCount; i++)
            {
                // Заголовки
                FieldName[i] = System.Text.Encoding.Default.GetString(buffer, i * 32, 10).TrimEnd(new char[] { (char)0x00 });
                FieldType[i] = "" + (char)buffer[i * 32 + 11];
                FieldSize[i] = buffer[i * 32 + 16];
                FieldDigs[i] = buffer[i * 32 + 17];
                FieldsLength = FieldsLength + FieldSize[i];
                // Создаю колонки
                switch (FieldType[i])
                {
                    case "L":
                        {
                            dtTable.Columns.Add(FieldName[i], Type.GetType("System.Boolean"));
                            dclColumns.Add(FieldName[i], Type.GetType("System.Boolean"), FieldSize[i], FieldDigs[i]);
                            break;
                        }
                    case "D":
                        {
                            dtTable.Columns.Add(FieldName[i], Type.GetType("System.DateTime"));
                            dclColumns.Add(FieldName[i], Type.GetType("System.DateTime"), FieldSize[i], FieldDigs[i]);
                            break;
                        }
                    case "N":
                        {
                            if (FieldDigs[i] == 0)
                            {
                                dtTable.Columns.Add(FieldName[i], Type.GetType("System.Int32"));
                                dclColumns.Add(FieldName[i], Type.GetType("System.Int32"), FieldSize[i], FieldDigs[i]);
                            }
                            else
                            {
                                dtTable.Columns.Add(FieldName[i], Type.GetType("System.Decimal"));
                                dclColumns.Add(FieldName[i], Type.GetType("System.Decimal"), FieldSize[i], FieldDigs[i]);
                            }
                            break;
                        }
                    case "F":
                        {
                            dtTable.Columns.Add(FieldName[i], Type.GetType("System.Double"));
                            dclColumns.Add(FieldName[i], Type.GetType("System.Double"), FieldSize[i], FieldDigs[i]);
                            break;
                        }
                    default:
                        {
                            dtTable.Columns.Add(FieldName[i], Type.GetType("System.String"));
                            dclColumns.Add(FieldName[i], Type.GetType("System.String"), FieldSize[i], FieldDigs[i]);
                            break;
                        }
                }
            }
            FS.ReadByte(); // Пропускаю разделитель схемы и данных
            System.Globalization.DateTimeFormatInfo dfi = new System.Globalization.CultureInfo("en-US", false).DateTimeFormat;
            System.Globalization.NumberFormatInfo nfi = new System.Globalization.CultureInfo("en-US", false).NumberFormat;
            buffer = new byte[FieldsLength];
            dtTable.BeginLoadData();
            for (int j = 0; j < RowsCount; j++)
            {
                FS.ReadByte(); // Пропускаю стартовый байт элемента данных
                FS.Read(buffer, 0, buffer.Length);
                System.Data.DataRow R = dtTable.NewRow();
                int Index = 0;
                for (int i = 0; i < FieldCount; i++)
                {
                    string l = System.Text.Encoding.GetEncoding(Encoding).GetString(buffer, Index, FieldSize[i]).TrimEnd(new char[] { (char)0x00 }).TrimEnd(new char[] { (char)0x20 });
                    Index = Index + FieldSize[i];
                    if (l != "")
                        switch (FieldType[i])
                        {
                            case "L": R[i] = l == "T" ? true : false; break;
                            case "D": R[i] = DateTime.ParseExact(l, "yyyyMMdd", dfi); break;
                            case "N":
                                {
                                    if (FieldDigs[i] == 0)
                                        R[i] = int.Parse(l, nfi);
                                    else
                                        R[i] = decimal.Parse(l, nfi);
                                    break;
                                }
                            case "F": R[i] = double.Parse(l, nfi); break;
                            default: R[i] = l; break;
                        }
                    else
                        R[i] = DBNull.Value;
                }
                dtTable.Rows.Add(R);
                Application.DoEvents();
            }
            dtTable.EndLoadData();
            FS.Close();
            return dtTable;
        }

        public void LoadTableHeader(string FileName)
        {
            this.dtTable.Clear();
            this.dclColumns.Clear();
            this.dtTable.TableName = strTableName;
            System.IO.FileStream FS = new System.IO.FileStream(FileName, System.IO.FileMode.Open);
            byte[] buffer = new byte[4]; // Кол-во записей: 4 байтa, начиная с 5-го
            FS.Position = 4; FS.Read(buffer, 0, buffer.Length);
            int RowsCount = buffer[0] + (buffer[1] * 0x100) + (buffer[2] * 0x10000) + (buffer[3] * 0x1000000);
            buffer = new byte[2]; // Кол-во полей: 2 байтa, начиная с 9-го
            FS.Position = 8; FS.Read(buffer, 0, buffer.Length);
            int FieldCount = (((buffer[0] + (buffer[1] * 0x100)) - 1) / 32) - 1;
            FS.Position = 29;
            int Encoding = FS.ReadByte(); // Кодировка
            switch (Encoding)
            {
                case 0x26:
                    Encoding = 866;
                    break;
                case 0xC9:
                    Encoding = 1251;
                    break;
                default: break;
            }
            string[] FieldName = new string[FieldCount]; // Массив названий полей
            string[] FieldType = new string[FieldCount]; // Массив типов полей
            byte[] FieldSize = new byte[FieldCount]; // Массив размеров полей
            byte[] FieldDigs = new byte[FieldCount]; // Массив размеров дробной части
            buffer = new byte[32 * FieldCount]; // Описание полей: 32 байта * кол-во, начиная с 33-го
            FS.Position = 32; FS.Read(buffer, 0, buffer.Length);
            int FieldsLength = 0;
            for (int i = 0; i < FieldCount; i++)
            {
                // Заголовки
                FieldName[i] = System.Text.Encoding.Default.GetString(buffer, i * 32, 10).TrimEnd(new char[] { (char)0x00 });
                FieldType[i] = "" + (char)buffer[i * 32 + 11];
                FieldSize[i] = buffer[i * 32 + 16];
                FieldDigs[i] = buffer[i * 32 + 17];
                FieldsLength = FieldsLength + FieldSize[i];
                // Создаю колонки
                switch (FieldType[i])
                {
                    case "L":
                        {
                            dtTable.Columns.Add(FieldName[i], Type.GetType("System.Boolean"));
                            dclColumns.Add(FieldName[i], Type.GetType("System.Boolean"), FieldSize[i], FieldDigs[i]);
                            break;
                        }
                    case "D":
                        {
                            dtTable.Columns.Add(FieldName[i], Type.GetType("System.DateTime"));
                            dclColumns.Add(FieldName[i], Type.GetType("System.DateTime"), FieldSize[i], FieldDigs[i]);
                            break;
                        }
                    case "N":
                        {
                            if (FieldDigs[i] == 0)
                            {
                                dtTable.Columns.Add(FieldName[i], Type.GetType("System.Int32"));
                                dclColumns.Add(FieldName[i], Type.GetType("System.Int32"), FieldSize[i], FieldDigs[i]);
                            }
                            else
                            {
                                dtTable.Columns.Add(FieldName[i], Type.GetType("System.Decimal"));
                                dclColumns.Add(FieldName[i], Type.GetType("System.Decimal"), FieldSize[i], FieldDigs[i]);
                            }
                            break;
                        }
                    case "F":
                        {
                            dtTable.Columns.Add(FieldName[i], Type.GetType("System.Double"));
                            dclColumns.Add(FieldName[i], Type.GetType("System.Double"), FieldSize[i], FieldDigs[i]);
                            break;
                        }
                    default:
                        {
                            dtTable.Columns.Add(FieldName[i], Type.GetType("System.String"));
                            dclColumns.Add(FieldName[i], Type.GetType("System.String"), FieldSize[i], FieldDigs[i]);
                            break;
                        }

                }
            }
        }

        /// <summary>
        /// Append data from DataTable into end of *.DBF file.
        /// Automatically identify encoding [Russian DOS or Russian Windows]
        /// </summary>
        /// <param name="DT">DataTable object</param>
        /// <param name="FileName">Path to *.DBF file.</param>
        public void Append(string FileName)
        {
            System.IO.FileStream FS = new System.IO.FileStream(FileName, System.IO.FileMode.Open);
            byte[] buffer = new byte[4]; // Кол-во записей: 4 байтa, начиная с 5-го
            FS.Position = 4; FS.Read(buffer, 0, buffer.Length);
            int RowsCount = buffer[0] + (buffer[1] * 0x100) + (buffer[2] * 0x10000) + (buffer[3] * 0x1000000) + dtTable.Rows.Count;
            buffer = new byte[]{
                       (byte)(((RowsCount % 0x1000000) % 0x10000) % 0x100),
                       (byte)(((RowsCount % 0x1000000) % 0x10000) / 0x100),
                       (byte)(( RowsCount % 0x1000000) / 0x10000),
                       (byte)(  RowsCount / 0x1000000)
                      }; // Word32 -> кол-во строк 5-8 байты
            FS.Position = 4; FS.Write(buffer, 0, buffer.Length);
            buffer = new byte[2]; // Кол-во полей: 2 байтa, начиная с 9-го
            FS.Position = 8; FS.Read(buffer, 0, buffer.Length);
            int FieldCount = (((buffer[0] + (buffer[1] * 0x100)) - 1) / 32) - 1;
            FS.Position = 29;
            int Encoding = FS.ReadByte(); // Кодировка
            switch (Encoding)
            {
                case 0x26:
                    Encoding = 866;
                    break;
                case 0x65:
                    Encoding = 866;
                    break;
                case 0xC9:
                    Encoding = 1251;
                    break;
                default: break;
            }
            string[] FieldName = new string[FieldCount]; // Массив названий полей
            string[] FieldType = new string[FieldCount]; // Массив типов полей
            byte[] FieldSize = new byte[FieldCount]; // Массив размеров полей
            byte[] FieldDigs = new byte[FieldCount]; // Массив размеров дробной части
            buffer = new byte[32 * FieldCount]; // Описание полей: 32 байтa * кол-во, начиная с 33-го
            FS.Position = 32; FS.Read(buffer, 0, buffer.Length);
            int FieldsLength = 0;
            for (int i = 0; i < FieldCount; i++)
            {
                // Заголовки
                FieldName[i] = System.Text.Encoding.Default.GetString(buffer, i * 32, 10).TrimEnd(new char[] { (char)0x00 });
                FieldType[i] = "" + (char)buffer[i * 32 + 11];
                FieldSize[i] = buffer[i * 32 + 16];
                FieldDigs[i] = buffer[i * 32 + 17];
                FieldsLength = FieldsLength + FieldSize[i];
            }
            FS.Seek(-1, SeekOrigin.End);
            System.Globalization.DateTimeFormatInfo dfi = new System.Globalization.CultureInfo("en-US", false).DateTimeFormat;
            System.Globalization.NumberFormatInfo nfi = new System.Globalization.CultureInfo("en-US", false).NumberFormat;
            string Spaces = "";
            while (Spaces.Length < 255) Spaces = Spaces + " ";
            foreach (DataRow R in dtTable.Rows)
            {
                FS.WriteByte(0x20); // Пишу данные
                foreach (DataColumn C in dtTable.Columns)
                {
                    string l = R[C].ToString();
                    if (l != "") // Проверка на NULL
                    {
                        switch (FieldType[C.Ordinal])
                        {
                            case "L":
                                {
                                    l = bool.Parse(l).ToString();
                                    break;
                                }
                            case "N":
                                {
                                    l = decimal.Parse(l).ToString(nfi);
                                    break;
                                }
                            case "F":
                                {
                                    l = float.Parse(l).ToString(nfi);
                                    break;
                                }
                            case "D":
                                {
                                    l = DateTime.Parse(l).ToString("yyyyMMdd", dfi);
                                    break;
                                }
                            default: l = l.Trim() + Spaces; break;
                        }
                    }
                    else
                    {
                        if (FieldType[C.Ordinal] == "C"
                         || FieldType[C.Ordinal] == "D")
                            l = Spaces;
                    }
                    while (l.Length < FieldSize[C.Ordinal]) { l += (char)0x00; }
                    l = l.Substring(0, FieldSize[C.Ordinal]); // Корректирую размер
                    buffer = System.Text.Encoding.GetEncoding(Encoding).GetBytes(l); // Записываю в кодировке (MS-DOS Russian)
                    FS.Write(buffer, 0, buffer.Length);
                    Application.DoEvents();
                }
            }
            FS.WriteByte(0x1A); // Конец данных
            FS.Close();
        }
    }
}

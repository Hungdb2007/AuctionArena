using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
namespace AuctionArena.WinForms
{
    // XLSX theo Open Packaging Convention, không cần thêm thư viện Excel/NuGet.
    internal static class ExcelExporter
    {
        static void Part(ZipArchive zip, string name, Action<XmlWriter> build)
        {
            var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
            using (var stream = entry.Open())
            using (var writer = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false), CloseOutput = false
            }))
            {
                build(writer);
                writer.Flush();
            }
        }
        static void Txt(XmlWriter w, string element, string content)
        {
            w.WriteStartElement(element);
            w.WriteString(content??"");
            w.WriteEndElement();
        }
        static bool IsNumber(object value)
        {
            return value is decimal || value is double || value is float || value is int || value is long || value is short;
        }
        // Ngày lấy theo định dạng của bảng (dd/MM/yyyy HH:mm); số giữ nguyên giá trị.
        static object CellValue(DataGridViewCell cell)
        {
            var value = cell.Value;
            if (value is DateTime)
            {
                string format = cell.OwningColumn.DefaultCellStyle.Format;
                return ((DateTime)value).ToString(string.IsNullOrEmpty(format) ? Theme.DateTimeFormat : format);
            }
            return value;
        }
        public static void Export(DataGridView grid)
        {
            if (grid == null || grid.Rows.Count == 0)
            {
                throw new InvalidOperationException("Chưa có dữ liệu để xuất.");
            }
            using (var dlg = new SaveFileDialog
            {
                Filter = "Excel (*.xlsx)|*.xlsx", FileName = "AuctionArena_Export.xlsx"
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                using (var file = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write))
                using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
                {
                    Part(zip, "[Content_Types].xml", w => 
                    {
                        w.WriteStartDocument(); w.WriteStartElement("Types", "http://schemas.openxmlformats.org/package/2006/content-types");
                        w.WriteStartElement("Default"); w.WriteAttributeString("Extension", "rels"); w.WriteAttributeString("ContentType", "application/vnd.openxmlformats-package.relationships+xml"); w.WriteEndElement();
                        w.WriteStartElement("Default"); w.WriteAttributeString("Extension", "xml"); w.WriteAttributeString("ContentType", "application/xml"); w.WriteEndElement();
                        w.WriteStartElement("Override"); w.WriteAttributeString("PartName", "/xl/workbook.xml"); w.WriteAttributeString("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"); w.WriteEndElement();
                        w.WriteStartElement("Override"); w.WriteAttributeString("PartName", "/xl/worksheets/sheet1.xml"); w.WriteAttributeString("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"); w.WriteEndElement(); w.WriteEndElement();
                    });
                    Part(zip, "_rels/.rels", w => 
                    {
                        w.WriteStartDocument(); w.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");
                        w.WriteStartElement("Relationship"); w.WriteAttributeString("Id", "rId1"); w.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"); w.WriteAttributeString("Target", "xl/workbook.xml"); w.WriteEndElement(); w.WriteEndElement();
                    });
                    Part(zip, "xl/workbook.xml", w => 
                    {
                        w.WriteStartDocument(); w.WriteStartElement("workbook", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                        w.WriteStartElement("sheets"); w.WriteStartElement("sheet"); w.WriteAttributeString("name", "Du lieu"); w.WriteAttributeString("sheetId", "1"); w.WriteAttributeString("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships", "rId1"); w.WriteEndElement(); w.WriteEndElement(); w.WriteEndElement();
                    });
                    Part(zip, "xl/_rels/workbook.xml.rels", w => 
                    {
                        w.WriteStartDocument(); w.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");
                        w.WriteStartElement("Relationship"); w.WriteAttributeString("Id", "rId1"); w.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"); w.WriteAttributeString("Target", "worksheets/sheet1.xml"); w.WriteEndElement(); w.WriteEndElement();
                    });
                    Part(zip, "xl/worksheets/sheet1.xml", w => 
                    {
                        w.WriteStartDocument(); w.WriteStartElement("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"); w.WriteStartElement("sheetData");
                        // Cột theo đúng thứ tự và tiêu đề tiếng Việt đang hiển thị trên bảng.
                        var cols = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible && c.ValueType != typeof(byte[]))
                        .OrderBy(c => c.DisplayIndex).ToArray();
                        int row = 0;
                        Action<object[]> add = values =>
                        {
                            w.WriteStartElement("row"); w.WriteAttributeString("r", (++row).ToString());
                            foreach (var value in values)
                            {
                                w.WriteStartElement("c");
                                if (IsNumber(value))
                                {
                                    // Số giữ dạng số để Excel còn tính tổng/sắp xếp được.
                                    Txt(w, "v", Convert.ToString(value, CultureInfo.InvariantCulture));
                                }
                                else
                                {
                                    w.WriteAttributeString("t", "inlineStr"); w.WriteStartElement("is"); Txt(w, "t", Convert.ToString(value)); w.WriteEndElement();
                                }
                                w.WriteEndElement();
                            }
                            w.WriteEndElement();
                        };
                        add(cols.Select(x => (object)x.HeaderText).ToArray());
                        foreach (DataGridViewRow r in grid.Rows)
                        {
                            if (!r.IsNewRow)add(cols.Select(x => CellValue(r.Cells[x.Index])).ToArray());
                        }
                        w.WriteEndElement(); w.WriteEndElement();
                    });
                }
                MessageBox.Show("Đã xuất file Excel: "+dlg.FileName);
            }
        }
    }
}

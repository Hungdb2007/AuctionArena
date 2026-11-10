using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    // View hệ thống của SQL Server: mỗi dòng là một cột của một bảng.
    // Chỉ dùng để đọc (kiểm tra cấu trúc database bằng LINQ), không ghi.
    [Table("COLUMNS", Schema = "INFORMATION_SCHEMA")]
    public class SchemaColumnEntity
    {
        [Key, Column("TABLE_SCHEMA", Order = 0)]
        public string TableSchema { get; set; }
        [Key, Column("TABLE_NAME", Order = 1)]
        public string TableName { get; set; }
        [Key, Column("COLUMN_NAME", Order = 2)]
        public string ColumnName { get; set; }
    }
}

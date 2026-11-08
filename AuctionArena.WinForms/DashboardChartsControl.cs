using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using AuctionArena.Services;
namespace AuctionArena.WinForms
{
    // Hiển thị biểu đồ từ số liệu do Services cung cấp.
    public sealed class DashboardChartsControl : UserControl
    {
        public DashboardChartsControl(DashboardChartsData data)
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Bg;
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, 
                Padding = new Padding(8), BackColor = Theme.Bg
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            Controls.Add(layout);
            var bar = Create("Phí người mua đã thu theo tháng (không VAT)", SeriesChartType.Column);
            foreach (var m in data.Months)
            bar.Series[0].Points.AddXY(m.Month.ToString("00")+"/"+m.Year, m.Revenue);
            var pie = Create("Tỷ trọng giá búa theo loại hiện vật", SeriesChartType.Pie);
            foreach (var category in data.Categories.Where(x => x.Amount>0))
            pie.Series[0].Points.AddXY(category.Category, category.Amount);
            var top = Create("5 lot có giá búa cao nhất", SeriesChartType.Bar);
            foreach (var lot in data.TopLots)
            top.Series[0].Points.AddXY(lot.Item, lot.Price);
            layout.Controls.Add(bar, 0, 0);
            layout.Controls.Add(pie, 1, 0);
            layout.Controls.Add(top, 0, 1);
            layout.SetColumnSpan(top, 2);
            if (!data.Months.Any() && !data.Categories.Any() && !data.TopLots.Any())
            {
                var empty = new Label
                {
                    Text = "Chưa có dữ liệu thanh toán để hiển thị biểu đồ.", 
                    Dock = DockStyle.Bottom, Height = 26, ForeColor = Theme.Muted, 
                    TextAlign = ContentAlignment.MiddleCenter
                };
                Controls.Add(empty);
                empty.BringToFront();
            }
        }
        static Chart Create(string title, SeriesChartType type)
        {
            var chart = new Chart
            {
                Dock = DockStyle.Fill, Margin = new Padding(7), 
                BackColor = Theme.Panel, Palette = ChartColorPalette.BrightPastel
            };
            chart.ChartAreas.Add(new ChartArea("Area")
            {
                BackColor = Theme.Panel
            });
            chart.Titles.Add(new Title(title)
            {
                ForeColor = Theme.Text, Font = new Font("Segoe UI", 10, FontStyle.Bold)
            });
            var series = new Series("Dữ liệu")
            {
                ChartType = type, IsValueShownAsLabel = true, 
                LabelForeColor = Theme.Text
            };
            chart.Series.Add(series);
            chart.Legends.Add(new Legend
            {
                ForeColor = Theme.Text, BackColor = Theme.Panel
            });
            chart.ChartAreas[0].AxisX.LabelStyle.ForeColor = Theme.Muted;
            chart.ChartAreas[0].AxisY.LabelStyle.ForeColor = Theme.Muted;
            chart.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chart.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.FromArgb(219, 229, 239);
            return chart;
        }
    }
}

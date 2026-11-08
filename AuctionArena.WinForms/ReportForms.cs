using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AuctionArena.Services;
using Microsoft.Reporting.WinForms;
namespace AuctionArena.WinForms
{
    // Form báo cáo duy nhất; tất cả dữ liệu đi qua ReportService.
    public sealed class ReportForm : Form
    {
        private readonly ReportViewer viewer = new ReportViewer();
        public ReportForm(string title, string resource, string dataSetName, IEnumerable rows, bool allowPrint)
        {
            Text = title;
            Width = 1100;
            Height = 750;
            StartPosition = FormStartPosition.CenterParent;
            viewer.Dock = DockStyle.Fill;
            Controls.Add(viewer);
            viewer.ProcessingMode = ProcessingMode.Local;
            viewer.ShowPrintButton = allowPrint;
            viewer.ShowExportButton = allowPrint;
            viewer.LocalReport.ReportEmbeddedResource = resource;
            viewer.LocalReport.DataSources.Clear();
            viewer.LocalReport.DataSources.Add(new ReportDataSource(dataSetName, rows));
            // Chỉ dựng báo cáo sau khi dữ liệu và tên dataset được gán đầy đủ.
            viewer.SetDisplayMode(DisplayMode.PrintLayout);
            viewer.RefreshReport();
        }
        public static void OpenBidTranscript(IWin32Window parent, int actor, int lotId)
        {
            try
            {
                var service = new ReportService();
                var rows = service.BidTranscript(actor, lotId);
                if (rows.Count == 0)
                {
                    MessageBox.Show("Lot chưa có lượt trả giá để lập biên bản.");
                    return;
                }
                using (var f = new ReportForm("Biên bản trả giá", 
                "AuctionArena.WinForms.Reports.BidTranscript.rdlc", "BidTranscriptData", 
                rows, service.CanPrint(actor, "BIDS")))f.ShowDialog(parent);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể in biên bản trả giá: "+ex.Message);
            }
        }
        public static void OpenPaymentReceipt(IWin32Window parent, int actor, int paymentId)
        {
            try
            {
                var service = new ReportService();
                using (var f = new ReportForm("Phiếu thu thanh toán", 
                "AuctionArena.WinForms.Reports.PaymentReceipt.rdlc", "PaymentReceiptData", 
                service.PaymentReceipt(actor, paymentId), service.CanPrint(actor, "INVOICES")))f.ShowDialog(parent);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể in phiếu thu: "+ex.Message);
            }
        }
        public static void OpenSellerPayoutReceipt(IWin32Window parent, int actor, int payoutId)
        {
            try
            {
                var service = new ReportService();
                using (var f = new ReportForm("Phiếu chi chủ hàng", 
                "AuctionArena.WinForms.Reports.SellerPayoutReceipt.rdlc", "SellerPayoutData", 
                service.SellerPayoutReceipt(actor, payoutId), service.CanPrint(actor, "PAYOUTS")))f.ShowDialog(parent);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể in phiếu chi: "+ex.Message);
            }
        }
        public static void OpenItemLabel(IWin32Window parent, int actor, int id)
        {
            try
            {
                var service = new ItemLabelReportService();
                var rows = service.Get(actor, id);
                if (rows.Count == 0)
                {
                    MessageBox.Show("Không tìm thấy hiện vật.");
                    return;
                }
                using (var form = new ReportForm("Nhãn hiện vật", 
                "AuctionArena.WinForms.Reports.ItemLabel.rdlc", "ItemLabelData", 
                rows, service.CanPrint(actor)))form.ShowDialog(parent);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể in nhãn hiện vật: "+ex.Message, "Báo cáo");
            }
        }
        public static void OpenInvoice(IWin32Window parent, int actor, int id)
        {
            try
            {
                using (var form = new ReportForm("Hóa đơn đấu giá", "AuctionArena.WinForms.Reports.Invoice.rdlc", "InvoiceData", new ReportService().Invoice(actor, id), new ReportService().CanPrint(actor, "INVOICES"))) form.ShowDialog(parent);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể in hóa đơn: "+ex.Message, "Báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        public static void OpenContract(IWin32Window parent, int actor, int id)
        {
            try
            {
                var service = new ReportService();
                using (var form = new ReportForm("Hợp đồng ký gửi", 
                "AuctionArena.WinForms.Reports.Contract.rdlc", "ContractData", 
                service.Contract(actor, id), service.CanPrint(actor, "CONTRACTS")))
                form.ShowDialog(parent);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể in hợp đồng: "+ex.Message, "Báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        public static void OpenConsignorReturn(IWin32Window parent, int actor, int id)
        {
            try
            {
                var service = new ReportService();
                using (var form = new ReportForm("Biên bản trả hiện vật", 
                "AuctionArena.WinForms.Reports.ConsignorReturn.rdlc", "ConsignorReturnData", 
                service.ReturnToOwner(actor, id), service.CanPrint(actor, "STOCK_ISSUES")))form.ShowDialog(parent);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không in được biên bản: "+ex.Message, "Báo cáo");
            }
        }
        public static void OpenIssue(IWin32Window parent, int actor, int id)
        {
            try
            {
                using (var form = new ReportForm("Phiếu xuất kho", "AuctionArena.WinForms.Reports.StockIssue.rdlc", "IssueData", new ReportService().Issue(actor, id), new ReportService().CanPrint(actor, "STOCK_ISSUES"))) form.ShowDialog(parent);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể in phiếu xuất: "+ex.Message, "Báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        public static void OpenBusiness(IWin32Window parent, int actor, string kind, IList rows)
        {
            var named = new Dictionary<string, string>
            {
                {
                    "Revenue", "BusinessRevenue"
                }, 
                {
                    "Session", "BusinessSession"
                }, 
                {
                    "Category", "BusinessCategory"
                }, 
                {
                    "Debt", "BusinessDebt"
                }, 
                {
                    "Payout", "BusinessPayout"
                }
            };
            if (!named.ContainsKey(kind))
            {
                throw new BusinessException("Loại báo cáo không hợp lệ.");
            }
            if (rows == null || rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu theo điều kiện lọc.");
                return;
            }
            using (var form = new ReportForm("Báo cáo "+named[kind], 
            "AuctionArena.WinForms.Reports."+named[kind]+".rdlc", "BusinessReportData", 
            rows, new ReportService().CanPrint(actor, "REPORTS")))form.ShowDialog(parent);
        }
        public static void OpenRevenue(IWin32Window parent, int actor, DateTime from, DateTime to)
        {
            try
            {
                using (var form = new ReportForm("Báo cáo doanh thu", "AuctionArena.WinForms.Reports.Revenue.rdlc", "RevenueData", new ReportService().Revenue(actor, from, to), new ReportService().CanPrint(actor, "REPORTS"))) form.ShowDialog(parent);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể lập báo cáo: "+ex.Message, "Báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
    // Báo cáo chỉ đọc; quyền In quyết định thao tác in và xuất.
    public sealed class ReportNavigationForm : Form
    {
        readonly int actor;
        readonly BusinessReportService service = new BusinessReportService();
        readonly DateTimePicker from, to;
        readonly ComboBox kind, groupBy, session, category, consignor, tier;
        readonly DataGridView grid;
        readonly Label summary, explanation;
        List<BusinessReportRow> current = new List<BusinessReportRow>();
        public ReportNavigationForm(int actor)
        {
            this.actor = actor;
            Text = "Báo cáo kinh doanh – 5 loại";
            Width = 1200;
            Height = 830;
            MinimumSize = new System.Drawing.Size(1050, 730);
            StartPosition = FormStartPosition.CenterParent;
            Theme.Apply(this);
            Controls.Add(Theme.Label("Kỳ từ ngày", 20, 20, 220));
            Controls.Add(Theme.Label("Đến ngày", 235, 20, 210));
            from = new DateTimePicker
            {
                Left = 20, Top = 52, Width = 190, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1)
            };
            to = new DateTimePicker
            {
                Left = 235, Top = 52, Width = 190, Format = DateTimePickerFormat.Short
            };
            Controls.Add(from);
            Controls.Add(to);
            Controls.Add(Theme.Label("Loại báo cáo", 455, 20, 335));
            kind = new ComboBox
            {
                Left = 455, Top = 52, Width = 335, DropDownStyle = ComboBoxStyle.DropDownList
            };
            kind.Items.AddRange(new object[]
            {
                "Doanh thu tổng hợp", "Theo phiên", "Theo loại hiện vật", 
                "Công nợ chưa thanh toán", "Chi trả chủ hàng"
            });
            kind.SelectedIndex = 0;
            Controls.Add(kind);
            Controls.Add(Theme.Label("Nhóm theo", 815, 20, 285));
            groupBy = new ComboBox
            {
                Left = 815, Top = 52, Width = 305, DropDownStyle = ComboBoxStyle.DropDownList
            };
            groupBy.Items.AddRange(new object[]
            {
                "Ngày", "Tháng", "Phiên", "Loại hiện vật"
            });
            groupBy.SelectedIndex = 0;
            Controls.Add(groupBy);
            var shortcuts = new[]
            {
                "Hôm nay", "7 ngày", "Tháng này", "Quý này", "Năm nay"
            };
            for (int i = 0; i<shortcuts.Length; i++)
            {
                int ix = i;
                var b = Theme.Button(shortcuts[i], 20+i*130, 99, 123);
                Controls.Add(b);
                b.Click+=(sender, args) => SetRange(ix);
            }
            Controls.Add(Theme.Label("Phiên", 20, 155, 250));
            Controls.Add(Theme.Label("Loại hiện vật", 285, 155, 245));
            Controls.Add(Theme.Label("Chủ hàng", 550, 155, 245));
            Controls.Add(Theme.Label("Hạng khách", 815, 155, 245));
            session = Select(20, 185, 245);
            category = Select(285, 185, 245);
            consignor = Select(550, 185, 245);
            tier = Select(815, 185, 305);
            tier.Items.AddRange(new object[]
            {
                "Tất cả", "Normal", "Silver", "Gold", "VIP"
            });
            tier.SelectedIndex = 0;
            var preview = Theme.Button("Xem / Làm mới", 20, 231, 230);
            Controls.Add(preview);
            var print = Theme.Button("In / Xuất PDF", 265, 231, 220);
            Controls.Add(print);
            var export = Theme.Button("Xuất Excel (.xlsx)", 500, 231, 230);
            Controls.Add(export);
            var reset = Theme.Button("Xóa bộ lọc", 745, 231, 210);
            Controls.Add(reset);
            grid = Theme.Grid(20, 288, 1130, 405);
            grid.ReadOnly = true;
            grid.AutoGenerateColumns = true;
            grid.Anchor = AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;
            Controls.Add(grid);
            summary = new Label
            {
                Left = 20, Top = 704, Width = 1100, Height = 29, ForeColor = Theme.Text, 
                Anchor = AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom
            };
            Controls.Add(summary);
            explanation = new Label
            {
                Left = 20, Top = 733, Width = 1130, Height = 35, ForeColor = Theme.Muted, 
                Anchor = AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom, 
                Text = "Doanh thu theo ngày tất toán; công nợ theo ngày phát hành; chi trả theo ngày thực chi. " +
                "Tỉ lệ bán theo ngày kết thúc lot; công nợ lọc từng loại được phân bổ theo tỷ trọng giá búa."
            };
            Controls.Add(explanation);
            preview.Click+=(sender, args) => Theme.Safe(RefreshRows);
            print.Click+=(sender, args) => Theme.Safe(() => 
            {
                if (current.Count == 0)RefreshRows();
                if (current.Count>0)ReportForm.OpenBusiness(this, actor, Kind(), current);
            });
            export.Click+=(sender, args) => Theme.Safe(() => 
            {
                if (current.Count == 0)RefreshRows();
                if (current.Count>0)
                {
                    if (!new ReportService().CanPrint(actor, "REPORTS"))
                    throw new BusinessException("Tài khoản chưa có quyền In/Xuất báo cáo.");
                    ExcelExporter.Export(grid);
                }
            });
            reset.Click+=(sender, args) => 
            {
                session.SelectedIndex = category.SelectedIndex = consignor.SelectedIndex = tier.SelectedIndex = 0;
                kind.SelectedIndex = groupBy.SelectedIndex = 0;
                SetRange(2);
                Theme.Safe(RefreshRows);
            };
            kind.SelectedIndexChanged+=(sender, args) => groupBy.Enabled = kind.SelectedIndex == 0 || kind.SelectedIndex>=3;
            Shown+=(sender, args) => Theme.Safe(() => 
            {
                LoadFilters(); RefreshRows();
            });
        }
        ComboBox Select(int left, int top, int width)
        {
            var c = new ComboBox
            {
                Left = left, Top = top, Width = width, DropDownStyle = ComboBoxStyle.DropDownList
            };
            Controls.Add(c);
            return c;
        }
        void LoadOptions(ComboBox control, List<BusinessReportOption> values)
        {
            values.Insert(0, new BusinessReportOption
            {
                Id = 0, Name = "Tất cả"
            });
            control.DataSource = values;
            control.DisplayMember = "Name";
            control.ValueMember = "Id";
            control.SelectedIndex = 0;
        }
        void LoadFilters()
        {
            LoadOptions(session, service.Sessions(actor));
            LoadOptions(category, service.Categories(actor));
            LoadOptions(consignor, service.Consignors(actor));
        }
        string Kind()
        {
            return new[]
            {
                "Revenue", "Session", "Category", "Debt", "Payout"
            }
            [kind.SelectedIndex];
        }
        static int? SelectedId(ComboBox combo)
        {
            var option = combo.SelectedItem as BusinessReportOption;
            return option == null || option.Id == 0?(int?)null:option.Id;
        }
        BusinessReportFilter Filter()
        {
            if (from.Value.Date>to.Value.Date)
            {
                throw new BusinessException("Từ ngày phải trước hoặc bằng Đến ngày.");
            }
            return new BusinessReportFilter
            {
                From = from.Value.Date, To = to.Value.Date, 
                SessionId = SelectedId(session), CategoryId = SelectedId(category), ConsignorId = SelectedId(consignor), 
                MemberTier = tier.SelectedIndex == 0?null:tier.Text, 
                GroupBy = new[]
                {
                    "Day", "Month", "Session", "Category"
                }
                [groupBy.SelectedIndex]
            };
        }
        void RefreshRows()
        {
            current = service.Get(actor, Kind(), Filter());
            grid.DataSource = null;
            grid.DataSource = current;
            foreach (string name in new[]
            {
                "GroupLabel", "HammerTotal", "BuyerFee", "SellerFee", "Discount", 
                "Vat", "PlatformRevenue", "UnpaidAmount", "PaidToSeller", "SoldLots", "UnsoldLots", "InvoiceCount", "Outcome"
            })
            {
                var col = grid.Columns[name];
                if (col == null)continue;
                col.Width = name == "GroupLabel"?225:name == "Outcome"?180:130;
                if (name != "GroupLabel" && name != "Outcome")col.DefaultCellStyle.Format = "N0";
                if (Kind() == "Debt")col.Visible = new[]
                {
                    "GroupLabel", "UnpaidAmount", "InvoiceCount"
                }.Contains(name);
                else if (Kind() == "Payout")col.Visible = new[]
                {
                    "GroupLabel", "HammerTotal", "SellerFee", "PaidToSeller"
                }.Contains(name);
                else col.Visible = !new[]
                {
                    "UnpaidAmount", "PaidToSeller", "SoldLots", "UnsoldLots"
                }.Contains(name);
            }
            if (current.Count == 0)
            {
                summary.Text = "Không có dữ liệu theo kỳ và bộ lọc đã chọn.";
                return;
            }
            summary.Text = "Dòng: "+current.Count+" | Giá búa: "+current.Sum(x => x.HammerTotal).ToString("N0")+
            " | Phí sàn: "+current.Sum(x => x.PlatformRevenue).ToString("N0")+
            " | Nợ: "+current.Sum(x => x.UnpaidAmount).ToString("N0")+
            " | Thực chi: "+current.Sum(x => x.PaidToSeller).ToString("N0")+" đ";
        }
        void SetRange(int n)
        {
            var today = DateTime.Today;
            if (n == 0)
            {
                from.Value = today;
                to.Value = today;
            }
            if (n == 1)
            {
                from.Value = today.AddDays(-6);
                to.Value = today;
            }
            if (n == 2)
            {
                from.Value = new DateTime(today.Year, today.Month, 1);
                to.Value = today;
            }
            if (n == 3)
            {
                from.Value = new DateTime(today.Year, ((today.Month-1)/3)*3+1, 1);
                to.Value = today;
            }
            if (n == 4)
            {
                from.Value = new DateTime(today.Year, 1, 1);
                to.Value = today;
            }
        }
    }
}

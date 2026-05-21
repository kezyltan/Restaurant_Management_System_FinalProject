using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace RestaurantInventoryStockMonitoringSystem
{
    public class MainForm : Form
    {
        private const string ApiBase = "http://localhost:8000/api.php";
        private const string AdminToken = "restaurant-admin-token-2026";

        private Panel rootPanel = new Panel();
        private DataGridView grid = new DataGridView();
        private TextBox searchBox = new TextBox();
        private ComboBox filterCategoryBox = new ComboBox();
        private ComboBox filterStatusBox = new ComboBox();
        private Label messageLabel = new Label();
        private Label detailLabel = new Label();
        private Label totalLabel = new Label();
        private Label pendingLabel = new Label();
        private Label purchaseLabel = new Label();
        private Label stockedLabel = new Label();

        private TextBox itemBox = new TextBox();
        private ComboBox categoryBox = new ComboBox();
        private TextBox quantityBox = new TextBox();
        private TextBox unitBox = new TextBox();
        private TextBox requestedByBox = new TextBox();
        private TextBox branchAreaBox = new TextBox();
        private ComboBox priorityBox = new ComboBox();
        private ComboBox statusBox = new ComboBox();
        private TextBox supplierBox = new TextBox();
        private TextBox remarksBox = new TextBox();

        private List<InventoryItem> allRecords = new List<InventoryItem>();
        private InventoryItem selectedRecord = null;
        private string currentUser = "";

        public MainForm()
        {
            Text = "Purchasing / Procurement System";
            Width = 1240;
            Height = 760;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1080, 680);
            LoadBackground();
            ShowLogin();
        }

        private void LoadBackground()
        {
            string bg = Path.Combine(Application.StartupPath, "assets", "restaurant_inventory_bg.png");
            if (File.Exists(bg))
            {
                BackgroundImage = Image.FromFile(bg);
                BackgroundImageLayout = ImageLayout.Stretch;
            }
            BackColor = Color.FromArgb(31, 46, 34);
        }

        private void ResetRoot()
        {
            Controls.Clear();
            rootPanel = new Panel();
            rootPanel.Dock = DockStyle.Fill;
            rootPanel.Padding = new Padding(28);
            rootPanel.BackColor = Color.FromArgb(31, 46, 34);
            Controls.Add(rootPanel);
        }

        private void ShowLogin()
        {
            ResetRoot();

            var card = new Panel();
            card.Width = 480;
            card.Height = 410;
            card.BackColor = Color.FromArgb(255, 248, 236);
            card.Left = (ClientSize.Width - card.Width) / 2;
            card.Top = (ClientSize.Height - card.Height) / 2;
            card.Anchor = AnchorStyles.None;
            card.Padding = new Padding(30);
            rootPanel.Controls.Add(card);

            var mark = new Label();
            mark.Text = "PO";
            mark.TextAlign = ContentAlignment.MiddleCenter;
            mark.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            mark.ForeColor = Color.White;
            mark.BackColor = Color.FromArgb(44, 80, 53);
            mark.SetBounds(30, 26, 62, 62);
            card.Controls.Add(mark);

            var title = new Label();
            title.Text = "Procurement Login";
            title.Font = new Font("Segoe UI", 23, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(31, 36, 26);
            title.SetBounds(110, 28, 320, 36);
            card.Controls.Add(title);

            var sub = new Label();
            sub.Text = "Purchasing system paired with the restaurant inventory PHP system";
            sub.Font = new Font("Segoe UI", 9);
            sub.ForeColor = Color.FromArgb(91, 80, 67);
            sub.SetBounds(112, 66, 330, 34);
            card.Controls.Add(sub);

            var userLabel = MakeFieldLabel("Username", 30, 124);
            var username = MakeTextBox(30, 150, false);
            var passLabel = MakeFieldLabel("Password", 30, 204);
            var password = MakeTextBox(30, 230, true);
            var signIn = MakeButton("Sign In to Procurement", 30, 294, 398, Color.FromArgb(44, 80, 53), Color.White);
            var note = new Label();
            note.Text = "Default account: procurement / procure123";
            note.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            note.ForeColor = Color.FromArgb(92, 84, 74);
            note.SetBounds(30, 350, 398, 22);

            signIn.Click += delegate
            {
                string user = username.Text.Trim();
                string pass = password.Text.Trim();
                if (user == "procurement" && pass == "procure123")
                {
                    currentUser = user;
                    ShowProcurementSystem();
                    return;
                }
                MessageBox.Show("Invalid procurement account.", "Sign In", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };

            password.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    signIn.PerformClick();
                }
            };

            card.Controls.Add(userLabel);
            card.Controls.Add(username);
            card.Controls.Add(passLabel);
            card.Controls.Add(password);
            card.Controls.Add(signIn);
            card.Controls.Add(note);
        }

        private Label MakeFieldLabel(string text, int x, int y)
        {
            var label = new Label();
            label.Text = text;
            label.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(39, 38, 30);
            label.SetBounds(x, y, 260, 22);
            return label;
        }

        private TextBox MakeTextBox(int x, int y, bool password)
        {
            var box = new TextBox();
            box.Font = new Font("Segoe UI", 12);
            box.SetBounds(x, y, 398, 34);
            box.PasswordChar = password ? '*' : '\0';
            return box;
        }

        private Button MakeButton(string text, int x, int y, int width, Color back, Color fore)
        {
            var button = new Button();
            button.Text = text;
            button.SetBounds(x, y, width, 38);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = back;
            button.ForeColor = fore;
            button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            return button;
        }

        private void ShowProcurementSystem()
        {
            ResetRoot();

            var shell = new Panel();
            shell.Dock = DockStyle.Fill;
            shell.BackColor = Color.FromArgb(255, 248, 236);
            shell.Padding = new Padding(20);
            rootPanel.Controls.Add(shell);

            var split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.FixedPanel = FixedPanel.Panel1;
            split.SplitterDistance = 390;
            split.SplitterWidth = 8;
            split.BackColor = Color.FromArgb(255, 248, 236);
            shell.Controls.Add(split);

            messageLabel.Dock = DockStyle.Bottom;
            messageLabel.Height = 34;
            messageLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            messageLabel.ForeColor = Color.FromArgb(56, 73, 42);
            messageLabel.BackColor = Color.FromArgb(255, 248, 236);
            messageLabel.TextAlign = ContentAlignment.MiddleLeft;
            shell.Controls.Add(messageLabel);

            var filters = BuildFilters();
            shell.Controls.Add(filters);

            var header = BuildHeader();
            shell.Controls.Add(header);

            var formPanel = BuildProcurementForm();
            split.Panel1.Controls.Add(formPanel);

            var rightPanel = new Panel();
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.BackColor = Color.FromArgb(255, 248, 236);
            rightPanel.Padding = new Padding(8, 0, 0, 0);
            split.Panel2.Controls.Add(rightPanel);

            detailLabel.Dock = DockStyle.Bottom;
            detailLabel.Height = 90;
            detailLabel.Font = new Font("Segoe UI", 9);
            detailLabel.ForeColor = Color.FromArgb(64, 59, 51);
            detailLabel.BackColor = Color.FromArgb(246, 239, 226);
            detailLabel.Padding = new Padding(12);
            detailLabel.Text = "Select a purchase request to view details and perform procurement actions.";
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.MultiSelect = false;
            grid.BackgroundColor = Color.FromArgb(255, 252, 246);
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.GridColor = Color.FromArgb(229, 216, 197);
            grid.DefaultCellStyle.BackColor = Color.FromArgb(255, 252, 246);
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(30, 43, 29);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(90, 122, 73);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 80, 53);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.EnableHeadersVisualStyles = false;
            grid.Font = new Font("Segoe UI", 9);
            grid.CellClick += Grid_CellClick;
            grid.SelectionChanged += Grid_SelectionChanged;
            rightPanel.Controls.Add(grid);
            rightPanel.Controls.Add(detailLabel);

            ClearForm();
            RefreshList();
        }

        private Panel BuildHeader()
        {
            var header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 132;
            header.BackColor = Color.FromArgb(44, 80, 53);
            header.Padding = new Padding(20);

            var title = new Label();
            title.Text = "Purchasing / Procurement System";
            title.Font = new Font("Segoe UI", 22, FontStyle.Bold);
            title.ForeColor = Color.White;
            title.AutoSize = true;
            title.Location = new Point(20, 18);
            header.Controls.Add(title);

            var subtitle = new Label();
            subtitle.Text = "Signed in as " + currentUser + " - manage restaurant purchase requests from the PHP inventory system";
            subtitle.Font = new Font("Segoe UI", 10);
            subtitle.ForeColor = Color.FromArgb(230, 246, 218);
            subtitle.AutoSize = true;
            subtitle.Location = new Point(22, 58);
            header.Controls.Add(subtitle);

            var stats = new FlowLayoutPanel();
            stats.FlowDirection = FlowDirection.LeftToRight;
            stats.WrapContents = false;
            stats.AutoSize = true;
            stats.Location = new Point(20, 86);
            stats.BackColor = Color.Transparent;
            header.Controls.Add(stats);

            totalLabel = MakeStatLabel("Total: 0");
            pendingLabel = MakeStatLabel("Pending: 0");
            purchaseLabel = MakeStatLabel("For Purchase: 0");
            stockedLabel = MakeStatLabel("Stocked: 0");
            stats.Controls.Add(totalLabel);
            stats.Controls.Add(pendingLabel);
            stats.Controls.Add(purchaseLabel);
            stats.Controls.Add(stockedLabel);

            var signOut = MakeButton("Sign Out", 0, 0, 110, Color.FromArgb(201, 145, 53), Color.FromArgb(36, 28, 16));
            signOut.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            signOut.Location = new Point(header.Width - 135, 24);
            signOut.Click += delegate { currentUser = ""; ShowLogin(); };
            header.Controls.Add(signOut);
            header.Resize += delegate { signOut.Location = new Point(header.Width - 135, 24); };

            return header;
        }

        private Label MakeStatLabel(string text)
        {
            var label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(39, 38, 30);
            label.BackColor = Color.FromArgb(255, 248, 236);
            label.Padding = new Padding(10, 4, 10, 4);
            label.Margin = new Padding(0, 0, 8, 0);
            return label;
        }

        private Panel BuildFilters()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Top;
            panel.Height = 92;
            panel.BackColor = Color.FromArgb(255, 248, 236);
            panel.Padding = new Padding(0, 18, 0, 10);

            var filters = new FlowLayoutPanel();
            filters.Dock = DockStyle.Fill;
            filters.FlowDirection = FlowDirection.LeftToRight;
            filters.WrapContents = true;
            filters.BackColor = Color.FromArgb(255, 248, 236);
            panel.Controls.Add(filters);

            searchBox.Width = 220;
            searchBox.Font = new Font("Segoe UI", 10);
            searchBox.Margin = new Padding(0, 0, 10, 0);
            searchBox.Text = "";
            searchBox.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ApplyLocalFilter();
                    e.SuppressKeyPress = true;
                }
            };

            filterCategoryBox.Width = 145;
            filterCategoryBox.DropDownStyle = ComboBoxStyle.DropDownList;
            filterCategoryBox.Font = new Font("Segoe UI", 10);
            filterCategoryBox.Items.Clear();
            filterCategoryBox.Items.AddRange(new object[] { "All Categories", "Meat", "Vegetables", "Seafood", "Condiments", "Beverages", "Packaging", "Cleaning Supply", "Other" });
            filterCategoryBox.SelectedIndex = 0;
            filterCategoryBox.Margin = new Padding(0, 0, 10, 0);

            filterStatusBox.Width = 145;
            filterStatusBox.DropDownStyle = ComboBoxStyle.DropDownList;
            filterStatusBox.Font = new Font("Segoe UI", 10);
            filterStatusBox.Items.Clear();
            filterStatusBox.Items.AddRange(new object[] { "All Status", "Pending", "Approved", "For Purchase", "Stocked", "Rejected", "Cancelled" });
            filterStatusBox.SelectedIndex = 0;
            filterStatusBox.Margin = new Padding(0, 0, 10, 0);

            var searchButton = MakeButton("Search", 0, 0, 85, Color.FromArgb(44, 80, 53), Color.White);
            var refreshButton = MakeButton("Refresh", 0, 0, 88, Color.FromArgb(201, 145, 53), Color.FromArgb(36, 28, 16));
            var clearButton = MakeButton("Clear", 0, 0, 75, Color.FromArgb(92, 84, 74), Color.White);

            searchButton.Click += delegate { ApplyLocalFilter(); };
            refreshButton.Click += delegate { RefreshList(); };
            clearButton.Click += delegate { ClearFilters(); ApplyLocalFilter(); };

            filters.Controls.Add(MakeInlineText("Search requests:"));
            filters.Controls.Add(searchBox);
            filters.Controls.Add(filterCategoryBox);
            filters.Controls.Add(filterStatusBox);
            filters.Controls.Add(searchButton);
            filters.Controls.Add(refreshButton);
            filters.Controls.Add(clearButton);

            return panel;
        }

        private Label MakeInlineText(string text)
        {
            var label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(39, 38, 30);
            label.Padding = new Padding(0, 8, 5, 0);
            label.Margin = new Padding(0, 0, 4, 0);
            return label;
        }

        private Panel BuildProcurementForm()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Color.FromArgb(246, 239, 226);
            panel.Padding = new Padding(16);
            panel.AutoScroll = true;

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Top;
            layout.AutoSize = true;
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.RowCount = 2;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(layout);

            var title = new Label();
            title.Text = "Purchase Request Details";
            title.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(35, 45, 31);
            title.Dock = DockStyle.Fill;
            title.Height = 34;
            layout.Controls.Add(title, 0, 0);
            layout.SetColumnSpan(title, 2);

            var help = new Label();
            help.Text = "Create, update, approve, order, and receive restaurant inventory requests.";
            help.Font = new Font("Segoe UI", 9);
            help.ForeColor = Color.FromArgb(92, 84, 74);
            help.Dock = DockStyle.Fill;
            help.Height = 40;
            layout.Controls.Add(help, 0, 1);
            layout.SetColumnSpan(help, 2);

            itemBox = MakeDockTextBox();
            categoryBox = MakeDockCombo(new object[] { "Meat", "Vegetables", "Seafood", "Condiments", "Beverages", "Packaging", "Cleaning Supply", "Other" });
            quantityBox = MakeDockTextBox();
            unitBox = MakeDockTextBox();
            requestedByBox = MakeDockTextBox();
            branchAreaBox = MakeDockTextBox();
            priorityBox = MakeDockCombo(new object[] { "Low", "Normal", "High", "Urgent" });
            statusBox = MakeDockCombo(new object[] { "Pending", "Approved", "For Purchase", "Stocked", "Rejected", "Cancelled" });
            supplierBox = MakeDockTextBox();
            remarksBox = MakeDockTextBox();
            remarksBox.Multiline = true;
            remarksBox.Height = 82;
            remarksBox.ScrollBars = ScrollBars.Vertical;

            AddFullField(layout, "Item / Supply Name", itemBox);
            AddHalfFields(layout, "Category", categoryBox, "Priority", priorityBox);
            AddHalfFields(layout, "Quantity", quantityBox, "Unit", unitBox);
            AddHalfFields(layout, "Requested By", requestedByBox, "Department / Branch Area", branchAreaBox);
            AddHalfFields(layout, "Preferred Supplier", supplierBox, "Procurement Status", statusBox);
            AddFullField(layout, "Remarks / Purchase Notes", remarksBox);

            var actionPanel = new FlowLayoutPanel();
            actionPanel.Dock = DockStyle.Fill;
            actionPanel.AutoSize = true;
            actionPanel.FlowDirection = FlowDirection.LeftToRight;
            actionPanel.WrapContents = true;
            actionPanel.Padding = new Padding(0, 8, 0, 0);
            int actionRow = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(actionPanel, 0, actionRow);
            layout.SetColumnSpan(actionPanel, 2);

            var newButton = MakeButton("New", 0, 0, 84, Color.FromArgb(92, 84, 74), Color.White);
            var saveButton = MakeButton("Save Request", 0, 0, 132, Color.FromArgb(44, 80, 53), Color.White);
            var deleteButton = MakeButton("Delete", 0, 0, 90, Color.FromArgb(182, 78, 57), Color.White);
            var approveButton = MakeButton("Approve", 0, 0, 98, Color.FromArgb(56, 116, 58), Color.White);
            var orderButton = MakeButton("Mark For Purchase", 0, 0, 154, Color.FromArgb(201, 145, 53), Color.FromArgb(36, 28, 16));
            var receiveButton = MakeButton("Mark Stocked / Received", 0, 0, 184, Color.FromArgb(41, 96, 84), Color.White);

            newButton.Margin = new Padding(0, 0, 8, 8);
            saveButton.Margin = new Padding(0, 0, 8, 8);
            deleteButton.Margin = new Padding(0, 0, 8, 8);
            approveButton.Margin = new Padding(0, 0, 8, 8);
            orderButton.Margin = new Padding(0, 0, 8, 8);
            receiveButton.Margin = new Padding(0, 0, 8, 8);

            newButton.Click += delegate { ClearForm(); };
            saveButton.Click += delegate { SaveCurrentRecord(); };
            deleteButton.Click += delegate { DeleteSelectedRecord(); };
            approveButton.Click += delegate { ChangeSelectedStatus("Approved"); };
            orderButton.Click += delegate { ChangeSelectedStatus("For Purchase"); };
            receiveButton.Click += delegate { ChangeSelectedStatus("Stocked"); };

            actionPanel.Controls.Add(newButton);
            actionPanel.Controls.Add(saveButton);
            actionPanel.Controls.Add(deleteButton);
            actionPanel.Controls.Add(approveButton);
            actionPanel.Controls.Add(orderButton);
            actionPanel.Controls.Add(receiveButton);

            return panel;
        }

        private TextBox MakeDockTextBox()
        {
            var box = new TextBox();
            box.Font = new Font("Segoe UI", 10);
            box.Dock = DockStyle.Fill;
            box.Margin = new Padding(0, 0, 8, 8);
            return box;
        }

        private ComboBox MakeDockCombo(object[] items)
        {
            var combo = new ComboBox();
            combo.Font = new Font("Segoe UI", 10);
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Dock = DockStyle.Fill;
            combo.Margin = new Padding(0, 0, 8, 8);
            combo.Items.AddRange(items);
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
            return combo;
        }

        private Label MakeDockLabel(string text)
        {
            var label = new Label();
            label.Text = text;
            label.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(39, 38, 30);
            label.Dock = DockStyle.Fill;
            label.Height = 22;
            label.Margin = new Padding(0, 6, 8, 0);
            return label;
        }

        private void AddFullField(TableLayoutPanel layout, string label, Control control)
        {
            int row = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var lbl = MakeDockLabel(label);
            layout.Controls.Add(lbl, 0, row);
            layout.SetColumnSpan(lbl, 2);

            row = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(control, 0, row);
            layout.SetColumnSpan(control, 2);
        }

        private void AddHalfFields(TableLayoutPanel layout, string label1, Control control1, string label2, Control control2)
        {
            int row = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(MakeDockLabel(label1), 0, row);
            layout.Controls.Add(MakeDockLabel(label2), 1, row);

            row = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(control1, 0, row);
            layout.Controls.Add(control2, 1, row);
        }

        private void ClearFilters()
        {
            searchBox.Text = "";
            filterCategoryBox.SelectedIndex = 0;
            filterStatusBox.SelectedIndex = 0;
        }

        private void ClearForm()
        {
            selectedRecord = null;
            itemBox.Text = "";
            categoryBox.SelectedIndex = 0;
            quantityBox.Text = "";
            unitBox.Text = "";
            requestedByBox.Text = "";
            branchAreaBox.Text = "";
            priorityBox.SelectedItem = "Normal";
            statusBox.SelectedItem = "Pending";
            supplierBox.Text = "";
            remarksBox.Text = "";
            detailLabel.Text = "New purchase request. Complete the fields, then click Save Request.";
            if (grid.Rows.Count > 0) grid.ClearSelection();
        }

        private void RefreshList()
        {
            try
            {
                string url = ApiBase + "?action=admin_list&token=" + Uri.EscapeDataString(AdminToken);
                string json = Get(url);
                var serializer = new JavaScriptSerializer();
                var response = serializer.Deserialize<ApiListResponse>(json);
                if (response == null || !response.success || response.data == null)
                {
                    messageLabel.Text = "Unable to load purchase request records.";
                    return;
                }
                allRecords = response.data;
                ApplyLocalFilter();
                UpdateStats();
                messageLabel.Text = "Loaded " + allRecords.Count + " restaurant inventory request(s) for procurement review.";
            }
            catch (Exception ex)
            {
                messageLabel.Text = "Connection failed: " + ex.Message;
                MessageBox.Show("Cannot connect to the PHP restaurant inventory system. Start the PHP server in the php-api folder first.\n\nCommand: php -S localhost:8000", "Connection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ApplyLocalFilter()
        {
            string search = searchBox.Text.Trim().ToLowerInvariant();
            string category = filterCategoryBox.SelectedIndex > 0 ? filterCategoryBox.SelectedItem.ToString() : "";
            string status = filterStatusBox.SelectedIndex > 0 ? filterStatusBox.SelectedItem.ToString() : "";

            var filtered = new List<InventoryItem>();
            foreach (var item in allRecords)
            {
                bool match = true;
                if (search.Length > 0)
                {
                    string searchable = ((item.ingredient_name ?? "") + " " + (item.category ?? "") + " " + (item.requested_by ?? "") + " " + (item.branch_area ?? "") + " " + (item.remarks ?? "")).ToLowerInvariant();
                    match = searchable.Contains(search);
                }
                if (match && category.Length > 0)
                {
                    match = string.Equals(item.category, category, StringComparison.OrdinalIgnoreCase);
                }
                if (match && status.Length > 0)
                {
                    match = string.Equals(item.status, status, StringComparison.OrdinalIgnoreCase);
                }
                if (match) filtered.Add(item);
            }

            grid.DataSource = null;
            grid.DataSource = filtered;
            FormatGrid();
            if (filtered.Count == 0)
            {
                detailLabel.Text = "No purchase requests match the current filters.";
            }
            messageLabel.Text = "Showing " + filtered.Count + " of " + allRecords.Count + " request(s).";
        }

        private void UpdateStats()
        {
            int pending = 0;
            int forPurchase = 0;
            int stocked = 0;
            foreach (var item in allRecords)
            {
                if (string.Equals(item.status, "Pending", StringComparison.OrdinalIgnoreCase)) pending++;
                if (string.Equals(item.status, "For Purchase", StringComparison.OrdinalIgnoreCase)) forPurchase++;
                if (string.Equals(item.status, "Stocked", StringComparison.OrdinalIgnoreCase)) stocked++;
            }
            totalLabel.Text = "Total: " + allRecords.Count;
            pendingLabel.Text = "Pending: " + pending;
            purchaseLabel.Text = "For Purchase: " + forPurchase;
            stockedLabel.Text = "Stocked: " + stocked;
        }

        private void FormatGrid()
        {
            if (grid.Columns.Contains("id")) grid.Columns["id"].HeaderText = "ID";
            if (grid.Columns.Contains("ingredient_name")) grid.Columns["ingredient_name"].HeaderText = "Item / Supply";
            if (grid.Columns.Contains("category")) grid.Columns["category"].HeaderText = "Category";
            if (grid.Columns.Contains("quantity")) grid.Columns["quantity"].HeaderText = "Qty";
            if (grid.Columns.Contains("unit")) grid.Columns["unit"].HeaderText = "Unit";
            if (grid.Columns.Contains("requested_by")) grid.Columns["requested_by"].HeaderText = "Requested By";
            if (grid.Columns.Contains("branch_area")) grid.Columns["branch_area"].HeaderText = "Department / Area";
            if (grid.Columns.Contains("priority")) grid.Columns["priority"].HeaderText = "Priority";
            if (grid.Columns.Contains("remarks")) grid.Columns["remarks"].HeaderText = "Notes";
            if (grid.Columns.Contains("status")) grid.Columns["status"].HeaderText = "Procurement Status";
            if (grid.Columns.Contains("created_at")) grid.Columns["created_at"].HeaderText = "Created";
            if (grid.Columns.Contains("updated_at")) grid.Columns["updated_at"].HeaderText = "Updated";

            if (grid.Columns.Contains("id")) grid.Columns["id"].FillWeight = 42;
            if (grid.Columns.Contains("quantity")) grid.Columns["quantity"].FillWeight = 55;
            if (grid.Columns.Contains("unit")) grid.Columns["unit"].FillWeight = 65;
            if (grid.Columns.Contains("remarks")) grid.Columns["remarks"].FillWeight = 150;
        }

        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) LoadSelectedRow();
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (grid.Focused) LoadSelectedRow();
        }

        private void LoadSelectedRow()
        {
            if (grid.CurrentRow == null || grid.CurrentRow.DataBoundItem == null) return;
            var item = grid.CurrentRow.DataBoundItem as InventoryItem;
            if (item == null) return;

            selectedRecord = item;
            itemBox.Text = item.ingredient_name ?? "";
            SelectCombo(categoryBox, item.category);
            quantityBox.Text = item.quantity ?? "";
            unitBox.Text = item.unit ?? "";
            requestedByBox.Text = item.requested_by ?? "";
            branchAreaBox.Text = item.branch_area ?? "";
            SelectCombo(priorityBox, item.priority);
            SelectCombo(statusBox, item.status);
            supplierBox.Text = ExtractSupplier(item.remarks ?? "");
            remarksBox.Text = RemoveSupplierLine(item.remarks ?? "");

            string qty = (item.quantity ?? "") + " " + (item.unit ?? "");
            detailLabel.Text = "Selected Request #" + item.id + " | " + (item.ingredient_name ?? "") + " | " + qty +
                " | Status: " + (item.status ?? "") + " | Priority: " + (item.priority ?? "") +
                " | Area: " + (item.branch_area ?? "");
        }

        private void SelectCombo(ComboBox combo, string value)
        {
            if (value == null) value = "";
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (string.Equals(combo.Items[i].ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private bool ValidateForm()
        {
            if (itemBox.Text.Trim().Length == 0 || quantityBox.Text.Trim().Length == 0 || unitBox.Text.Trim().Length == 0 || requestedByBox.Text.Trim().Length == 0 || branchAreaBox.Text.Trim().Length == 0)
            {
                MessageBox.Show("Please complete Item, Quantity, Unit, Requested By, and Department / Branch Area.", "Missing Details", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private NameValueCollection BuildPostValues(string action)
        {
            var values = new NameValueCollection();
            values["action"] = action;
            values["token"] = AdminToken;
            if (selectedRecord != null) values["id"] = selectedRecord.id.ToString();
            values["ingredient_name"] = itemBox.Text.Trim();
            values["category"] = categoryBox.SelectedItem.ToString();
            values["quantity"] = quantityBox.Text.Trim();
            values["unit"] = unitBox.Text.Trim();
            values["requested_by"] = requestedByBox.Text.Trim();
            values["branch_area"] = branchAreaBox.Text.Trim();
            values["priority"] = priorityBox.SelectedItem.ToString();
            values["status"] = statusBox.SelectedItem.ToString();
            values["remarks"] = BuildRemarksForStorage();
            return values;
        }

        private string BuildRemarksForStorage()
        {
            string supplier = supplierBox.Text.Trim();
            string notes = remarksBox.Text.Trim();
            if (supplier.Length > 0)
            {
                if (notes.Length > 0) return "Preferred Supplier: " + supplier + Environment.NewLine + notes;
                return "Preferred Supplier: " + supplier;
            }
            return notes;
        }

        private string ExtractSupplier(string remarks)
        {
            if (remarks == null) return "";
            string[] lines = remarks.Replace("\r\n", "\n").Split('\n');
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line.StartsWith("Preferred Supplier:", StringComparison.OrdinalIgnoreCase))
                {
                    return line.Substring("Preferred Supplier:".Length).Trim();
                }
            }
            return "";
        }

        private string RemoveSupplierLine(string remarks)
        {
            if (remarks == null) return "";
            string[] lines = remarks.Replace("\r\n", "\n").Split('\n');
            var kept = new List<string>();
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (!line.StartsWith("Preferred Supplier:", StringComparison.OrdinalIgnoreCase))
                {
                    kept.Add(rawLine);
                }
            }
            return string.Join(Environment.NewLine, kept.ToArray()).Trim();
        }

        private void SaveCurrentRecord()
        {
            if (!ValidateForm()) return;
            try
            {
                string action = selectedRecord == null ? "add" : "update";
                var response = Post(ApiBase, BuildPostValues(action));
                if (response == null || !response.success)
                {
                    MessageBox.Show(response != null ? response.message : "Save failed.", "Save Request", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                messageLabel.Text = selectedRecord == null ? "Purchase request created." : "Purchase request updated.";
                RefreshList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to save the purchase request.\n\n" + ex.Message, "Save Request", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteSelectedRecord()
        {
            if (selectedRecord == null)
            {
                MessageBox.Show("Select a purchase request to delete.", "Delete Request", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var confirm = MessageBox.Show("Delete purchase request #" + selectedRecord.id + " for " + selectedRecord.ingredient_name + "?", "Delete Request", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            try
            {
                var values = new NameValueCollection();
                values["action"] = "delete";
                values["token"] = AdminToken;
                values["id"] = selectedRecord.id.ToString();
                var response = Post(ApiBase, values);
                if (response == null || !response.success)
                {
                    MessageBox.Show(response != null ? response.message : "Delete failed.", "Delete Request", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                ClearForm();
                RefreshList();
                messageLabel.Text = "Purchase request deleted.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to delete the purchase request.\n\n" + ex.Message, "Delete Request", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ChangeSelectedStatus(string newStatus)
        {
            if (selectedRecord == null)
            {
                MessageBox.Show("Select a purchase request first.", "Procurement Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SelectCombo(statusBox, newStatus);
            SaveCurrentRecord();
        }

        private string Get(string url)
        {
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                return client.DownloadString(url);
            }
        }

        private ApiSimpleResponse Post(string url, NameValueCollection values)
        {
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                byte[] responseBytes = client.UploadValues(url, "POST", values);
                string json = Encoding.UTF8.GetString(responseBytes);
                var serializer = new JavaScriptSerializer();
                return serializer.Deserialize<ApiSimpleResponse>(json);
            }
        }
    }

    public class InventoryItem
    {
        public int id { get; set; }
        public string ingredient_name { get; set; }
        public string category { get; set; }
        public string quantity { get; set; }
        public string unit { get; set; }
        public string requested_by { get; set; }
        public string branch_area { get; set; }
        public string priority { get; set; }
        public string remarks { get; set; }
        public string status { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
    }

    public class ApiListResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public List<InventoryItem> data { get; set; }
    }

    public class ApiSimpleResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public int id { get; set; }
    }
}

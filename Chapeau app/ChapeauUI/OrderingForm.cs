﻿using Model;
using Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChapeauUI
{
    public partial class OrderingForm : Form
    {
        private MenuService menuService = new MenuService();
        private OrderItemService orderItemService = new OrderItemService();
        private OrderService orderService = new OrderService();
        private BillService billService = new BillService();
        private EmployeeService employeeService = new EmployeeService();
        public OrderingForm()
        {
            InitializeComponent();
            InitializeMenus();
            InitializeComboBoxes();
            SetupListViewMouseEvents();
        }

        private void InitializeMenus()
        {
            ShowMenu(1, new string[] { "Starter", "Main", "Dessert" }, new ListView[] { listVStartersLunch, listVMainsLunch, listVDessertsLunch });
        }

        private void InitializeComboBoxes()
        {
            comboBoxGuests.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxTables.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void SetupListViewMouseEvents()
        {
            var listViews = new ListView[] { listVStartersLunch, listVMainsLunch, listVDessertsLunch,
                                             listVStartersDinner, listVMainsDinner, listVEntremetsDinner, listVDessertsDinner,
                                             listVSoftDrinks, listVBeers, listVWines, listVSpirit, listVCoffee };

            foreach (var listView in listViews)
            {
                listView.MouseDown += ListView_MouseDown;
            }
        }

        private void ShowMenu(int menuId, string[] parts, ListView[] listViews)
        {
            HideAll();

            switch (menuId)
            {
                case 1:
                    pnlLunch.Show();
                    break;
                case 2:
                    pnlDinner.Show();
                    break;
                case 3:
                    pnlDrinks.Show();
                    break;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                var menuItems = menuService.GetPartMenu(menuId, parts[i]);
                ShowMenuPart(menuItems, listViews[i]);
            }
        }

        private void ShowMenuPart(List<MenuItem> items, ListView listView)
        {
            listView.Items.Clear();

            foreach (MenuItem item in items)
            {
                ListViewItem listViewItem = new ListViewItem(item.Name);
                listViewItem.SubItems.Add(item.Name);
                listViewItem.SubItems.Add(item.Price.ToString("0.00€"));
                listViewItem.SubItems.Add(item.Stock.ToString());
                listView.Items.Add(listViewItem);
            }
        }

        private void HideAll()
        {
            pnlLunch.Hide();
            pnlDinner.Hide();
            pnlDrinks.Hide();
        }
        private void AddToOrder(ListViewItem item)
        {
            bool itemFound = false;

            foreach (ListViewItem orderItem in listVOrder.Items)
            {
                int currentQuantity = int.Parse(orderItem.SubItems[3].Text);
                int stockQuantity = orderItemService.GetOrderItemStock(orderItem.SubItems[1].Text);

                if (orderItem.Text == item.Text)
                {
                    if (currentQuantity >= stockQuantity)
                    {
                        MessageBox.Show("There are no more items in stock!", "Stock Limit Reached", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    orderItem.SubItems[3].Text = (int.Parse(orderItem.SubItems[3].Text) + 1).ToString();
                    itemFound = true;
                    break;
                }
            }

            if (!itemFound)
            {
                ListViewItem orderItem = new ListViewItem(item.Text);
                orderItem.SubItems.Add(item.SubItems[1].Text);
                orderItem.SubItems.Add(item.SubItems[2].Text);
                orderItem.SubItems.Add("1");
                orderItem.SubItems.Add("");
                listVOrder.Items.Add(orderItem);
            }
        }

        private void ListView_MouseDown(object sender, MouseEventArgs e)
        {
            ListView listView = sender as ListView;
            if (listView != null)
            {
                ListViewHitTestInfo hitTest = listView.HitTest(e.Location);
                if (hitTest.Item != null)
                {
                    AddToOrder(hitTest.Item);
                }
            }
        }

        private void btnClearAll_Click(object sender, EventArgs e)
        {
            ClearElements();
        }

        private void btnPlus_Click(object sender, EventArgs e)
        {
            if (listVOrder.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = listVOrder.SelectedItems[0];

                int currentQuantity = int.Parse(selectedItem.SubItems[3].Text);
                int stockQuantity = orderItemService.GetOrderItemStock(selectedItem.SubItems[1].Text);

                if (currentQuantity < stockQuantity)
                {
                    selectedItem.SubItems[3].Text = (currentQuantity + 1).ToString();
                }
                else
                {
                    MessageBox.Show("There are no more items in stock!", "Stock Limit Reached", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select an item!", "No Item Selected", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnMinus_Click(object sender, EventArgs e)
        {
            if (listVOrder.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = listVOrder.SelectedItems[0];
                int currentAmount = int.Parse(selectedItem.SubItems[3].Text);

                if (currentAmount > 1)
                {
                    selectedItem.SubItems[3].Text = (currentAmount - 1).ToString();
                }
                else
                {
                    listVOrder.Items.Remove(selectedItem);
                }
            }
            else
            {
                MessageBox.Show("Please select an item!", "No Item Selected", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnFinishOrder_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs())
                return;

            int selectedTable = int.Parse(comboBoxTables.SelectedItem.ToString());

            int billId;
            if (billService.BillExistsForTable(selectedTable))
            {
                billId = billService.GetBillIdByTable(selectedTable);
            }
            else
            {
                int guestNumber = comboBoxGuests.SelectedIndex + 1;
                billId = CreateNewBill(selectedTable, guestNumber);
            }

            List<int> orderIds = CreateNewOrder(billId);
            AddOrderItems(orderIds);

            ClearElements();
            RefreshPannels();
            MessageBox.Show("Order was added successfully!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool ValidateInputs()
        {
            if (comboBoxTables.SelectedItem == null)
            {
                MessageBox.Show("Select a table!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (comboBoxGuests.SelectedItem == null)
            {
                MessageBox.Show("Select a number of guests!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private int CreateNewBill(int selectedTable, int guestNumber)
        {
            int billId = billService.GetNextBillId();
            billService.AddBill(new Bill(billId, 0, 0, guestNumber, selectedTable, " ", 0));
            return billId;
        }

        private List<int> CreateNewOrder(int billId)
        {
            int preparationTime = CountPreparationTime();
            List<int> menuIds = CheckNumberOfMenu();
            List<int> orderIds = new List<int>();

            bool containsKitchenItems = menuIds.Contains(1) || menuIds.Contains(2);
            bool containsBarItems = menuIds.Contains(3);

            if (containsKitchenItems)
            {
                int kitchenOrderId = orderService.GetNextOrderId();
                orderService.AddOrder(new Order(kitchenOrderId, DateTime.Now, preparationTime, OrderStatus.Placed, billId, employeeService.GetIdByRole("waiter"), "Kitchen"));
                orderIds.Add(kitchenOrderId);
            }

            if (containsBarItems)
            {
                int barOrderId = orderService.GetNextOrderId();
                orderService.AddOrder(new Order(barOrderId, DateTime.Now, preparationTime, OrderStatus.Placed, billId, employeeService.GetIdByRole("waiter"), "Bar"));
                orderIds.Add(barOrderId);
            }

            return orderIds;
        }

        private void AddOrderItems(List<int> orderIds)
        {
            foreach (ListViewItem item in listVOrder.Items)
            {
                string itemName = item.SubItems[1].Text;
                int amount = int.Parse(item.SubItems[3].Text);
                int menuItemId = menuService.GetMenuItemByName(itemName);
                OrderStatus status = OrderStatus.Placed;
                string comment = item.SubItems[4].Text;
                int menuId = orderItemService.GetMenuIdByName(itemName);
                int orderId = orderIds.Count == 2 && (menuId == 1 || menuId == 2) ? orderIds[0] : orderIds.Last();

                orderItemService.RefreshOrderItemStock(itemName, amount);
                orderItemService.AddOrderItem(new OrderItem(orderId, menuItemId, amount, status, comment));
            }
        }

        private List<int> CheckNumberOfMenu()
        {
            HashSet<int> menuIds = new HashSet<int>();
            foreach (ListViewItem item in listVOrder.Items)
            {
                menuIds.Add(orderItemService.GetMenuIdByName(item.SubItems[1].Text));
            }
            return menuIds.ToList();
        }

        private int CountPreparationTime()
        {
            int preparationTime = 0;
            foreach (ListViewItem item in listVOrder.Items)
            {
                preparationTime += menuService.GetPreparationTimeByName(item.SubItems[1].Text);
            }
            return preparationTime;
        }

        private void RefreshPannels()
        {
            ShowMenu(1, new string[] { "Starter", "Main", "Dessert" }, new ListView[] { listVStartersLunch, listVMainsLunch, listVDessertsLunch });
        }

        private void ClearElements()
        {
            listVOrder.Items.Clear();
            comboBoxGuests.SelectedIndex = -1;
            comboBoxTables.SelectedIndex = -1;
            textBoxComment.Clear();
        }

        private void btnLunchM_Click(object sender, EventArgs e)
        {
            ShowMenu(1, new string[] { "Starter", "Main", "Dessert" }, new ListView[] { listVStartersLunch, listVMainsLunch, listVDessertsLunch });
        }

        private void btnDinnerM_Click(object sender, EventArgs e)
        {
            ShowMenu(2, new string[] { "Starter", "Main", "Entremet", "Dessert" }, new ListView[] { listVStartersDinner, listVMainsDinner, listVEntremetsDinner, listVDessertsDinner });
        }

        private void btnDrinksM_Click(object sender, EventArgs e)
        {
            ShowMenu(3, new string[] { "Soft Drink", "Beer", "Wine", "Spirit Drink", "Coffee / Tea" }, new ListView[] { listVSoftDrinks, listVSpirit, listVBeers, listVWines, listVCoffee });
        }

        private void btnAddCom_Click(object sender, EventArgs e)
        {
            if (listVOrder.SelectedItems.Count > 0)
            {
                if (textBoxComment.Text == string.Empty)
                {
                    MessageBox.Show("Please write a comment!", "No Item Selected", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (textBoxComment.Text.Length > 50)
                {
                    MessageBox.Show("Your comment is too long!", "No Item Selected", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                ListViewItem selectedItem = listVOrder.SelectedItems[0];
                selectedItem.SubItems[4].Text = textBoxComment.Text;
            }
            else
            {
                MessageBox.Show("Please select an item!", "No Item Selected", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRemoveCom_Click(object sender, EventArgs e)
        {
            if (listVOrder.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = listVOrder.SelectedItems[0];
                selectedItem.SubItems[4].Text = "";
            }
            else
            {
                MessageBox.Show("Please select an item!", "No Item Selected", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLogOut_Click(object sender, EventArgs e)
        {
            LoginForm loginForm = new LoginForm();
            this.Hide();
            loginForm.Closed += (s, args) => this.Close();
            loginForm.Show();

        }
    }
}

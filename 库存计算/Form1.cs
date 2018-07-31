using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace 库存计算
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }


        private void btnOpenOrder_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Excel文件(*.xls*)|*.xls*";
            dialog.RestoreDirectory = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string[] pathArray = dialog.FileNames;
                if (pathArray.Length > 1 || pathArray.Length == 0)
                {
                    MessageBox.Show("请选择一个文件!");
                }
                else
                {
                    txtOrder.Text = dialog.FileName;
                }
            }
        }

        private void btnOpenInventory_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Excel文件(*.xls*)|*.xls*";
            dialog.RestoreDirectory = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string[] pathArray = dialog.FileNames;
                if (pathArray.Length > 1 || pathArray.Length == 0)
                {
                    MessageBox.Show("请选择一个文件!");
                }
                else
                {
                    txtInventory.Text = dialog.FileName;
                }
            }
        }

        private void btnTransfer_Click(object sender, EventArgs e)
        {
            try
            {
                btnTransfer.Text = "计算中...";
                lblInfo.Text = "";
                lblOutput.Text = "";
                btnOpenOrder.Enabled = false;
                btnOpenInventory.Enabled = false;
                btnTransfer.Enabled = false;
                btnOutput.Enabled = false;
                txtOrder.Enabled = false;
                txtInventory.Enabled = false;
                txtOutput.Enabled = false;

                if (string.IsNullOrEmpty(txtOrder.Text))
                {
                    MessageBox.Show("请先选择订单文件!");
                }
                if (string.IsNullOrEmpty(txtInventory.Text))
                {
                    MessageBox.Show("请先选择库存文件!");
                }
                else if (string.IsNullOrEmpty(txtOutput.Text))
                {
                    MessageBox.Show("请选择保存路径!");
                }
                else
                {
                    string outputPath = GetFilePath(txtOutput.Text);
                    if (!Directory.Exists(outputPath))
                        Directory.CreateDirectory(outputPath);

                    var orderDt = new ExcelHelper(txtOrder.Text).ExcelToDataTable();
                    if (orderDt.Rows.Count == 0)
                    {
                        MessageBox.Show("订单文件为空!");
                        return;
                    }
                    var inventoryDt = new ExcelHelper(txtInventory.Text).ExcelToDataTable();
                    if (inventoryDt.Rows.Count == 0)
                    {
                        MessageBox.Show("库存文件为空!");
                        return;
                    }
                    orderDt.Columns.Add("是否发货");
                    inventoryDt.Columns.Add("已发数量", typeof(int));
                    inventoryDt.Columns.Add("剩余库存", typeof(int));

                    //订单excel排序
                    var tempOrderDt = orderDt.Clone();
                    foreach (DataRow item in orderDt.AsEnumerable().OrderBy(m => m.Field<string>("选项代码")).ThenBy(m => Convert.ToDateTime(m.Field<string>("汇款日"))))
                    {
                        tempOrderDt.ImportRow(item);
                    }

                    //保存最终结果
                    var resultOrderDt = tempOrderDt.Clone();
                    resultOrderDt.Columns["订购号码"].DataType = typeof(long);
                    resultOrderDt.Columns["购物车号码"].DataType = typeof(long);
                    resultOrderDt.Columns["商品代码"].DataType = typeof(long);
                    resultOrderDt.Columns["数量"].DataType = typeof(int);
                    foreach (DataRow item in tempOrderDt.AsEnumerable())
                    {
                        if (String.IsNullOrEmpty(item["选项代码"].ToString()) && String.IsNullOrEmpty(item["数量"].ToString()))
                            continue;
                        var inventoryRow = inventoryDt.AsEnumerable().FirstOrDefault(m => m.Field<string>("款号") == item["选项代码"].ToString());
                        if (inventoryRow != null)
                        {
                            var totalCount = int.Parse(inventoryRow["数量"].ToString());
                            var usedCount = String.IsNullOrEmpty(inventoryRow["已发数量"].ToString()) ? 0 : int.Parse(inventoryRow["已发数量"].ToString());
                            var orderCount = int.Parse(item["数量"].ToString());
                            if (orderCount <= (totalCount - usedCount))
                            {
                                item["是否发货"] = "是(当前库存" + (totalCount - usedCount) + ")";
                                inventoryRow["已发数量"] = usedCount + orderCount;
                                inventoryRow["剩余库存"] = totalCount - (usedCount + orderCount);
                                resultOrderDt.ImportRow(item);
                            }
                        }
                    }

                    var resultInventoryDt = inventoryDt.Clone();
                    resultInventoryDt.Columns["数量"].DataType = typeof(int);
                    foreach (DataRow inventoryRow in inventoryDt.AsEnumerable())
                    {
                        if (String.IsNullOrEmpty(inventoryRow["数量"].ToString()) && String.IsNullOrEmpty(inventoryRow["款号"].ToString()))
                            continue;
                        inventoryRow["已发数量"] = String.IsNullOrEmpty(inventoryRow["已发数量"].ToString()) ? 0 : int.Parse(inventoryRow["已发数量"].ToString());
                        inventoryRow["剩余库存"] = String.IsNullOrEmpty(inventoryRow["剩余库存"].ToString()) ? int.Parse(inventoryRow["数量"].ToString()) : int.Parse(inventoryRow["剩余库存"].ToString());
                        resultInventoryDt.ImportRow(inventoryRow);
                    }

                    if (resultOrderDt.Rows.Count > 0)
                    {
                        DataSet ds = new DataSet();
                        ds.Tables.Add(resultOrderDt);
                        ds.Tables.Add(resultInventoryDt);
                        new ExcelHelper(txtOutput.Text).DataSetToExcel(ds, true);
                        //MessageBox.Show("计算完成");
                        lblInfo.Text = "计算完成，点击打开：";
                        lblOutput.Text = txtOutput.Text;
                    }
                    else
                        MessageBox.Show("没有可以发货的款号");
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                btnTransfer.Text = "计算";
                btnOpenOrder.Enabled = true;
                btnOpenInventory.Enabled = true;
                btnTransfer.Enabled = true;
                btnOutput.Enabled = true;
                txtOrder.Enabled = true;
                txtInventory.Enabled = true;
                txtOutput.Enabled = true;
                txtOrder.Focus();
            }
        }

        private void btnOutput_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Excel文件(*.xlsx)|*.xlsx";
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtOutput.Text = dialog.FileName;
            }
        }

        private void lblOutput_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", lblOutput.Text);
        }

        private string GetFilePath(string fullPath)
        {
            string[] pathInfo = fullPath.Split('\\');
            if (pathInfo.Length > 1)
            {
                int idx = fullPath.LastIndexOf('\\');
                string resultStr = fullPath.Substring(0, idx - 0);
                return resultStr;
            }

            return "";
        }
    }
}

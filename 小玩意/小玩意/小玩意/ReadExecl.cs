using ClosedXML.Excel;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using OfficeOpenXml;
using System.Data;
using System.IO;
using System.Reflection;
using System.Windows;
using 小玩意.Model;
using 小玩意.ViewModel;

namespace 小玩意
{
    class ReadExecl
    {
        /// <summary>
        /// 读取配置文件
        /// </summary>
        /// <returns></returns>
        public static ReadIniModel ReadIniFile()
        {
            // EPPlus 5.0+ 需要设置 LicenseContext
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var rowData = new ReadIniModel();
            
            var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {
                filePath += "\\ini\\配置文件.xlsx";
                FileInfo fileInfo = new FileInfo(filePath);


                if (!System.IO.File.Exists(filePath))
                {
                    ErrorViewModel.Errornotice("文件不存在", true, 1);
                    return null;
                }
                //读取Sheet1的数据
                using (var workbook = new XLWorkbook(filePath))
                {
                    // 获取第一个工作表（也可以按名称：workbook.Worksheet("Sheet1")）
                    var worksheet = workbook.Worksheet(1);

                    // 读取第一行作为标题
                    var headerRow = worksheet.Row(1); // 第1行是标题
                    var headers = new List<string>();

                    // 遍历第一行所有有内容的单元格，提取标题
                    foreach (var cell in headerRow.CellsUsed())
                    {
                        headers.Add(cell.Value.ToString().Trim());
                    }

                    if (headers.Count == 0)
                    {
                        ErrorViewModel.Errornotice("❌ 第一行（标题行）为空，无法解析列名。",true,1);
                        return null;
                    }

                    // 从第二行开始读取数据
                    var dataRows = worksheet.RowsUsed().Skip(1); // 跳过标题行
                   
                    int rowIndex = 2; // 从 Excel 的第2行开始
                    foreach (var row in dataRows)
                    {
                        //Console.WriteLine($"📌 第 {rowIndex} 行数据：");
                        rowData.Sheet1.Add(new Tuple<string, string, string, string, string>(row.Cell(1).IsEmpty() ? "" : row.Cell(1).Value.ToString().Trim(), row.Cell(2).IsEmpty() ? "" : row.Cell(2).Value.ToString().Trim(), row.Cell(3).IsEmpty() ? "" : row.Cell(3).Value.ToString().Trim(), row.Cell(4).IsEmpty() ? "" : row.Cell(4).Value.ToString().Trim(), row.Cell(5).IsEmpty() ? "" : row.Cell(5).Value.ToString().Trim()));

                        rowIndex++;
                    }
                }
                //读取Sheet2的数据
                using (var workbook = new XLWorkbook(filePath))
                {
                    // 获取第二个工作表（也可以按名称：workbook.Worksheet("Sheet1")）
                    var worksheet = workbook.Worksheet(2);

                    // 读取第一行作为标题
                    var headerRow = worksheet.Row(1); // 第1行是标题
                    var headers = new List<string>();

                    // 遍历第一行所有有内容的单元格，提取标题
                    foreach (var cell in headerRow.CellsUsed())
                    {
                        headers.Add(cell.Value.ToString().Trim());
                    }

                    if (headers.Count == 0)
                    {
                        ErrorViewModel.Errornotice("❌ 第一行（标题行）为空，无法解析列名。", true, 1);
                        return null;
                    }

                    // 从第二行开始读取数据
                    var dataRows = worksheet.RowsUsed().Skip(1); // 跳过标题行

                    int rowIndex = 2; // 从 Excel 的第2行开始
                    foreach (var row in dataRows)
                    {
                        //Console.WriteLine($"📌 第 {rowIndex} 行数据：");
                        rowData.Sheet2.Add(new Tuple<string, string, string, string, string>(row.Cell(1).IsEmpty() ? "" : row.Cell(1).Value.ToString().Trim(), row.Cell(2).IsEmpty() ? "" : row.Cell(2).Value.ToString().Trim(), row.Cell(3).IsEmpty() ? "" : row.Cell(3).Value.ToString().Trim(), row.Cell(4).IsEmpty() ? "" : row.Cell(4).Value.ToString().Trim(), row.Cell(5).IsEmpty() ? "" : row.Cell(5).Value.ToString().Trim()));

                        rowIndex++;
                    }
                }
                //读取Sheet3的数据
                using (var workbook = new XLWorkbook(filePath))
                {
                    // 获取第三个工作表（也可以按名称：workbook.Worksheet("Sheet1")）
                    var worksheet = workbook.Worksheet(3);

                    // 读取第一行作为标题
                    var headerRow = worksheet.Row(1); // 第1行是标题
                    var headers = new List<string>();

                    // 遍历第一行所有有内容的单元格，提取标题
                    foreach (var cell in headerRow.CellsUsed())
                    {
                        headers.Add(cell.Value.ToString().Trim());
                    }

                    if (headers.Count == 0)
                    {
                        ErrorViewModel.Errornotice("❌ 第一行（标题行）为空，无法解析列名。", true, 1);
                        return null;
                    }

                    // 从第二行开始读取数据
                    var dataRows = worksheet.RowsUsed().Skip(1); // 跳过标题行

                    int rowIndex = 2; // 从 Excel 的第2行开始
                    foreach (var row in dataRows)
                    {
                        //Console.WriteLine($"📌 第 {rowIndex} 行数据：");
                        rowData.Sheet3.Add(new Tuple<string, string, string, string, string>(row.Cell(1).IsEmpty() ? "" : row.Cell(1).Value.ToString().Trim(), row.Cell(2).IsEmpty() ? "" : row.Cell(2).Value.ToString().Trim(), row.Cell(3).IsEmpty() ? "" : row.Cell(3).Value.ToString().Trim(), row.Cell(4).IsEmpty() ? "" : row.Cell(4).Value.ToString().Trim(), row.Cell(5).IsEmpty() ? "" : row.Cell(5).Value.ToString().Trim()));

                        rowIndex++;
                    }
                }
                //读取Sheet4的数据
                using (var workbook = new XLWorkbook(filePath))
                {
                    // 获取第四个工作表（也可以按名称：workbook.Worksheet("Sheet1")）
                    var worksheet = workbook.Worksheet(4);

                    // 读取第一行作为标题
                    var headerRow = worksheet.Row(1); // 第1行是标题
                    var headers = new List<string>();

                    // 遍历第一行所有有内容的单元格，提取标题
                    foreach (var cell in headerRow.CellsUsed())
                    {
                        headers.Add(cell.Value.ToString().Trim());
                    }

                    if (headers.Count == 0)
                    {
                        ErrorViewModel.Errornotice("❌ 第一行（标题行）为空，无法解析列名。", true, 1);
                        return null;
                    }

                    // 从第二行开始读取数据
                    var dataRows = worksheet.RowsUsed().Skip(1); // 跳过标题行

                    int rowIndex = 2; // 从 Excel 的第2行开始
                    foreach (var row in dataRows)
                    {
                        //Console.WriteLine($"📌 第 {rowIndex} 行数据：");
                        rowData.Sheet4.Add(new Tuple<string, string, string, string, string>(row.Cell(1).IsEmpty() ? "" : row.Cell(1).Value.ToString().Trim(), row.Cell(2).IsEmpty() ? "" : row.Cell(2).Value.ToString().Trim(), row.Cell(3).IsEmpty() ? "" : row.Cell(3).Value.ToString().Trim(), row.Cell(4).IsEmpty() ? "" : row.Cell(4).Value.ToString().Trim(), row.Cell(5).IsEmpty() ? "" : row.Cell(5).Value.ToString().Trim()));

                        rowIndex++;
                    }
                }
                //读取Sheet5的数据
                using (var workbook = new XLWorkbook(filePath))
                {
                    // 获取第五个工作表（也可以按名称：workbook.Worksheet("Sheet1")）
                    var worksheet = workbook.Worksheet(5);

                    // 读取第一行作为标题
                    var headerRow = worksheet.Row(1); // 第1行是标题
                    var headers = new List<string>();

                    // 遍历第一行所有有内容的单元格，提取标题
                    foreach (var cell in headerRow.CellsUsed())
                    {
                        headers.Add(cell.Value.ToString().Trim());
                    }

                    if (headers.Count == 0)
                    {
                        ErrorViewModel.Errornotice("❌ 第一行（标题行）为空，无法解析列名。", true, 1);
                        return null;
                    }

                    // 从第二行开始读取数据
                    var dataRows = worksheet.RowsUsed().Skip(1); // 跳过标题行

                    int rowIndex = 2; // 从 Excel 的第2行开始
                    foreach (var row in dataRows)
                    {
                        //Console.WriteLine($"📌 第 {rowIndex} 行数据：");
                        rowData.Sheet5.Add(new Tuple<string, string, string, string, string>(row.Cell(1).IsEmpty() ? "" : row.Cell(1).Value.ToString().Trim(), row.Cell(2).IsEmpty() ? "" : row.Cell(2).Value.ToString().Trim(), row.Cell(3).IsEmpty() ? "" : row.Cell(3).Value.ToString().Trim(), row.Cell(4).IsEmpty() ? "" : row.Cell(4).Value.ToString().Trim(), row.Cell(5).IsEmpty() ? "" : row.Cell(5).Value.ToString().Trim()));

                        rowIndex++;
                    }
                }

                return rowData;
            }
            catch
            {
                ErrorViewModel.Errornotice("文件已被占用或损坏请检查文件", true, 1);
                return rowData;
            }
        }


        public static void WriteDataXlsx(SaveModel value)
        {
            //这里直接在方法里面去异步写入
            Task.Factory.StartNew(() =>
            {
                //WriteExcel(value);

                // 创建 Excel 工作簿
                using (var workbook = new XLWorkbook())
                {
                    var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    //filePath += "\\Save\\";
                    var ws = workbook.Worksheets.Add("11");

                    //加载标题栏
                    for (int i = 0; i < value.Title_Row.Count; i++)
                    {
                        ws.Cell(1, i + 1).Value = value.Title_Row[i];
                        ws.Cell(1, i + 1).Style.Font.Bold = true;
                        ws.Cell(1, i + 1).Style.Font.FontName = "华文宋体";
                        ws.Cell(1, i + 1).Style.Font.FontSize = 20;
                        ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.AmberSaeEce;
                    }

                    // 填充数据 从第二行开始
                    int row = 2;
                    //这里是整个表格的数据处理 这里list有几个元素就代表这个表格有多少行
                    foreach (var emp in value.Data_Row)
                    {
                        //这里是每一行的数据处理 这里list有几个元素就代表这一行有多少列
                        int index = 1;
                        foreach (var item in emp)
                        {
                            ws.Cell(row, index).Value = item;
                            ws.Cell((int)row, index).Style.Font.FontName = "华文宋体";
                            ws.Cell((int)row, index).Style.Font.FontSize = 15;
                            ws.Cell((int)row, index).Style.Fill.BackgroundColor = XLColor.Green;
                            index++;
                        }
                        row++;
                    }

                    // 自动调整列宽
                    //ws.Columns().AdjustToContents();
                    ws.Columns("A:GZ").Width = 15;
                    ws.Rows(1, 1000).Height = 25;
                    // 设置表格边框（可选）
                    var range = ws.Range(1, 1, value.Data_Row.Count + 1, value.Title_Row.Count);
                    range.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
                    range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    // 保存文件
                    string SavefilePath = @$"D:\员工信息表{System.DateTime.Now.Month}.xlsx";
                    workbook.SaveAs(SavefilePath);

                    Application.Current.Dispatcher.Invoke(() =>
                        {
                            ErrorViewModel.Errornotice($"✅ Excel 表格已生成：{filePath}", false, 6);
                        });

                }
            });
        }

        /// <summary>
        /// 将excel中的数据导入到DataTable中  已弃用
        /// </summary>
        /// <param name="fileName">文件路径</param>
        /// <param name="sheetName">excel工作薄sheet的名称</param>
        /// <param name="isFirstRowColumn">第一行是否是DataTable的列名</param>
        /// <returns>返回的DataTable</returns>
        private static DataTable ExcelToDatatable(string fileName, string sheetName, bool isFirstRowColumn)
        {
            ISheet sheet = null;
            DataTable data = new DataTable();
            int startRow = 0;
            FileStream fs;
            IWorkbook workbook = null;
            int cellCount = 0;//列数
            int rowCount = 0;//行数
            try
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                if (fileName.IndexOf(".xlsx") > 0) // 2007版本
                {
                    workbook = new XSSFWorkbook(fs);
                }
                else if (fileName.IndexOf(".xls") > 0) // 2003版本
                {
                    workbook = new HSSFWorkbook(fs);
                }
                if (sheetName != null)
                {
                    sheet = workbook.GetSheet(sheetName);//根据给定的sheet名称获取数据
                }
                else
                {
                    //也可以根据sheet编号来获取数据
                    sheet = workbook.GetSheetAt(0);//获取第几个sheet表（此处表示如果没有给定sheet名称，默认是第一个sheet表）  
                }
                if (sheet != null)
                {
                    IRow firstRow = sheet.GetRow(0);
                    cellCount = firstRow.LastCellNum; //第一行最后一个cell的编号 即总的列数
                    if (isFirstRowColumn)//如果第一行是标题行
                    {
                        for (int i = firstRow.FirstCellNum; i < cellCount; ++i)//第一行列数循环
                        {
                            DataColumn column = new DataColumn(firstRow.GetCell(i).StringCellValue);//获取标题
                            data.Columns.Add(column);//添加列
                        }
                        startRow = sheet.FirstRowNum + 1;//1（即第二行，第一行0从开始）
                    }
                    else
                    {
                        startRow = sheet.FirstRowNum;//0
                    }
                    //最后一行的标号
                    rowCount = sheet.LastRowNum;
                    for (int i = startRow; i <= rowCount; ++i)//循环遍历所有行
                    {
                        IRow row = sheet.GetRow(i);//第几行
                        if (row == null)
                        {
                            continue; //没有数据的行默认是null;
                        }
                        //将excel表每一行的数据添加到datatable的行中
                        DataRow dataRow = data.NewRow();
                        for (int j = row.FirstCellNum; j < cellCount; ++j)
                        {
                            if (row.GetCell(j) != null) //同理，没有数据的单元格都默认是null
                            {
                                dataRow[j] = row.GetCell(j).ToString();
                            }
                        }
                        data.Rows.Add(dataRow);
                    }
                }
                return data;
            }
            catch (Exception ex)
            {
                //  Console.WriteLine("Exception: " + ex.Message);
                return null;
            }
        }
    }
}

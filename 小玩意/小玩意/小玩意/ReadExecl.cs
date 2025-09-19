using OfficeOpenXml;
using S7.Net.Types;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using OfficeOpenXml;
using ClosedXML.Excel;
using 小玩意.ViewModel;
using 小玩意.Model;
using System.Windows;

namespace 小玩意
{
    class ReadExecl
    {

        public static void ReadIniFile()
        {
            // EPPlus 5.0+ 需要设置 LicenseContext
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            //string ss = "2025/03/20;09:20:15;000;002;2550;0000;2550;0004;0001;00000;00000;0231;000;000;000;000;000;000;000;000;000;000;000;00000;000;000;0000;0000;0000;0000;0000;00000;00000;0000;000;000;000;000;000;000;000;000;000;000;000;32323;32338;32335;32331;32325;32337;32331;32331;32326;32333;32303;32316;32333;32341;32332;32336;32323;32319;32334;32339;32331;32338;32311;32343;32355;32358;32360;32360;32355;32360;32357;32356;32357;32327;32341;32360;32367;32359;32348;32340;32346;32355;32344;32359;32367;32328;32332;32355;32360;32351;32336;32355;32362;32353;32351;32353;32328;32324;32366;32354;32337;32354;32351;32364;32342;32343;32355;32364;32322;32334;32351;32353;32352;32364;32347;32360;32355;32357;32358;32341;32344;32357;32360;32361;32363;32342;32355;32368;32358;32335;32337;32336;32392;32405;32416;32424;32354;32423;32395;32427;32419;32421;32402;32400;32419;32424;32411;32431;32363;32433;32445;32425;32429;32423;32401;32338;32356;32349;32355;32361;32342;32359;32362;32349;32352;32330;32335;32355;32354;32350;32347;32356;32350;32358;32350;32343;32358;32339;32319;32337;32336;32327;32337;32327;32324;32335;32330;32326;32310;32328;32331;32332;32343;32334;32343;32336;32337;32346;32341;32330;32314;32334;32349;32347;32353;32356;32349;32365;32341;32341;32355;32338;32337;32352;32357;32349;32354;32359;32357;32351;32349;32356;32361;32343;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000;0000000;0000000;0000000;0000000;0000000;0000000;0000000;0000000;0;0";
            //string sss = "32 30 32 35 2f 30 33 2f 32 30 20 30 39 3a 32 30 3a 31 \r\n35 20 30 30 30 20 \r\n30 30 32 20 32 35 \r\n35 30 20 30 30 30 \r\n30 20 32 35 35 30 \r\n20 30 30 30 34 20 \r\n30 30 30 31 20 30 \r\n30 30 30 30 20 30 \r\n30 30 30 30 20 30 \r\n32 33 31 20 30 30 \r\n30 20 30 30 30 20 \r\n30 30 30 20 30 30 \r\n30 20 30 30 30 20 \r\n30 30 30 20 30 30 \r\n30 20 30 30 30 20 \r\n30 30 30 20 30 30 \r\n30 20 30 30 30 20 \r\n30 30 30 30 30 20 \r\n30 30 30 20 30 30 \r\n30 20 30 30 30 30 \r\n20 30 30 30 30 20 \r\n30 30 30 30 20 30 \r\n30 30 30 20 30 30 \r\n30 30 20 30 30 30 \r\n30 30 20 30 30 30 \r\n30 30 20 30 30 30 \r\n30 20 30 30 30 20 \r\n30 30 30 20 30 30 \r\n30 20 30 30 30 20 \r\n30 30 30 20 30 30 \r\n30 20 30 30 30 20 \r\n30 30 30 20 30 30 \r\n30 20 30 30 30 20 \r\n30 30 30 20 33 32 \r\n33 32 33 20 33 32\r\n33 33 38 20 33 32\r\n33 33 35 20 33 32 \r\n33 33 31 20 33 32 \r\n33 32 35 20 33 32 \r\n33 33 37 20 33 32 \r\n33 33 31 20 33 32 \r\n33 33 31 20 33 32 \r\n33 32 36 20 33 32 \r\n33 33 33 20 33 32 \r\n33 30 33 20 33 32 \r\n33 31 36 20 33 32 \r\n33 33 33 20 33 32 \r\n33 34 31 20 33 32 \r\n33 33 32 20 33 32 \r\n33 33 36 20 33 32 \r\n33 32 33 20 33 32 \r\n33 31 39 20 33 32 \r\n33 33 34 20 33 32 \r\n33 33 39 20 33 32 \r\n33 33 31 20 33 32 \r\n33 33 38 20 33 32 \r\n33 31 31 20 33 32 \r\n33 34 33 20 33 32 \r\n33 35 35 20 33 32 \r\n33 35 38 20 33 32 \r\n33 36 30 20 33 32 \r\n33 36 30 20 33 32 \r\n33 35 35 20 33 32 \r\n33 36 30 20 33 32 \r\n33 35 37 20 33 32 \r\n33 35 36 20 33 32 \r\n33 35 37 20 33 32";
            //string filePath = @"C:\path\to\your\file.xlsx";
            var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {
                filePath += "\\ini\\新建 Microsoft Excel 工作表.xlsx";
                FileInfo fileInfo = new FileInfo(filePath);


                if (!System.IO.File.Exists(filePath))
                {
                    ErrorViewModel.Errornotice("文件不存在", true, 1);
                    return;
                }

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
                        Console.WriteLine("❌ 第一行（标题行）为空，无法解析列名。");
                        return;
                    }

                    Console.WriteLine("✅ 检测到标题列：");
                    Console.WriteLine(string.Join(" | ", headers));
                    Console.WriteLine(new string('-', 50));

                    // 从第二行开始读取数据
                    var dataRows = worksheet.RowsUsed().Skip(1); // 跳过标题行
                    var rowData = new List<Tuple<string, string, string, string, string>>();
                    int rowIndex = 2; // 从 Excel 的第2行开始
                    foreach (var row in dataRows)
                    {
                        Console.WriteLine($"📌 第 {rowIndex} 行数据：");

                        // 创建一个字典来存储 "列名 -> 值"


                        //for (int i = 0; i < headers.Count; i++)
                        //{
                        //    // 获取当前列的单元格（列号 = i + 1）
                        //    var cell = row.Cell(i + 1); // Cell 是从 1 开始编号的
                        //    string value = cell.IsEmpty() ? "" : cell.Value.ToString().Trim();
                            rowData.Add(new Tuple<string, string, string, string, string>(row.Cell(1).IsEmpty()?"": row.Cell(1).Value.ToString().Trim(), row.Cell(2).IsEmpty() ? "" : row.Cell(2).Value.ToString().Trim(), row.Cell(3).IsEmpty() ? "" : row.Cell(3).Value.ToString().Trim(), row.Cell(4).IsEmpty() ? "" : row.Cell(4).Value.ToString().Trim(), row.Cell(5).IsEmpty() ? "" : row.Cell(5).Value.ToString().Trim()));
                        //}

                        // 输出每个字段
                        foreach (var kvp in rowData)
                        {
                            //Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                        }

                        Console.WriteLine(); // 空行分隔
                        rowIndex++;
                    }
                }
                    

                //ExcelToDatatable(filePath,"Sheet1",true);
                //using (ExcelPackage package = new ExcelPackage(fileInfo))
                //{
                //    // 获取第一个工作表
                //    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                //    // 获取表格维度（有效数据范围）
                //    int rowCount = worksheet.Dimension.Rows;
                //    int colCount = worksheet.Dimension.Columns;

                //    Console.WriteLine($"共 {rowCount} 行 {colCount} 列数据");
                //}
            }
            catch
            {
                ErrorViewModel.Errornotice("文件已被占用或损坏请检查文件",true,1);
            }
        }


        public static void WriteDataXlsx(SaveModel value)
        {
            

            // 创建 Excel 工作簿
            using (var workbook = new XLWorkbook())
            {
                var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                //filePath += "\\Save\\";
                var ws = workbook.Worksheets.Add("11");

                // 设置标题行
                //var headers = new[] { "ID", "姓名", "年龄", "部门", "薪资" };

                foreach (var item in value.Title_Row)
                {
                    //ws.Cell(1, i + 1).Value = item;
                    //ws.Cell(1, i + 1).Style.Font.Bold = true;
                    //ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                }
                //加载标题栏
                for (int i = 0; i < value.Title_Row.Count; i++)
                {
                    ws.Cell(1, i + 1).Value = value.Title_Row[i];
                    ws.Cell(1, i + 1).Style.Font.Bold = true;
                    ws.Cell(1, i + 1).Style.Font.FontName="华文宋体";
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
                ws.Rows(1,1000).Height = 25;
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

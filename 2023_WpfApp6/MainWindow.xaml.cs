﻿using LiveCharts;
using LiveCharts.Wpf;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace _2023_WpfApp6
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string defaultURL = "https://data.moenv.gov.tw/api/v2/aqx_p_432?api_key=e8dd42e6-9b8b-43f8-991e-b3dee723a52d&limit=1000&sort=ImportDate desc&format=JSON";
        AQIdata aqiData = new AQIdata();
        List<Field> fields = new List<Field>();
        List<Record> records = new List<Record>();
        SeriesCollection seriesCollection = new SeriesCollection();
        List<Record> selectedRecords = new List<Record>();
        public MainWindow()
        {
            InitializeComponent();
            UrlTextBox.Text = defaultURL;
            selectedRecords.Clear();
        }

        private async void fetchButton_Click(object sender, RoutedEventArgs e)
        {
            string url = UrlTextBox.Text;
            ContentTextBox.Text = "抓取資料...";

            string data = await FetchContentAsync(url);
            ContentTextBox.Text = data;
            aqiData = JsonSerializer.Deserialize<AQIdata>(data);
            fields = aqiData.fields.ToList();
            records = aqiData.records.ToList();
            selectedRecords = records;
            statusTextBlock.Text = $"共有 {records.Count} 筆資料，每筆資料有{fields.Count}個欄位。";

            DisplayAQIData();
        }

        private void DisplayAQIData()
        {
            RecordDataGrid.ItemsSource = records;

            Record record = records[0];
            DataWrapPanel.Children.Clear();

            foreach (Field field in fields)
            {
                var propertyInfo = record.GetType().GetProperty(field.id);
                if (propertyInfo != null)
                {
                    var value = propertyInfo.GetValue(record) as string;
                    if (double.TryParse(value, out double v))
                    {
                        CheckBox cb = new CheckBox
                        {
                            Content = field.info.label,
                            Tag = field.id,
                            Margin = new Thickness(3),
                            FontSize = 14,
                            FontWeight = FontWeights.Bold,
                            Width = 120
                        };
                        cb.Checked += UpdateChart;
                        cb.Unchecked += UpdateChart;
                        DataWrapPanel.Children.Add(cb);
                    }
                }
            }
        }

        private void UpdateChart(object sender, RoutedEventArgs e)
        {
            seriesCollection.Clear();

            foreach (CheckBox cb in DataWrapPanel.Children)
            {
                if (cb.IsChecked == true)
                {
                    List<String> labels = new List<String>();
                    String tag = cb.Tag as String;
                    ColumnSeries columnSeries=new ColumnSeries();
                    ChartValues<double> values = new ChartValues<double>();

                    foreach(Record record in selectedRecords)
                    {
                        var propertyInfo = record.GetType().GetProperty(tag);
                        if (propertyInfo != null)
                        {
                            var value = propertyInfo.GetValue(record) as string;
                            if (double.TryParse(value, out double v))
                            {
                                values.Add(v);
                                labels.Add(record.sitename);
                            }
                        }
                    }
                    columnSeries.Values = values;
                    columnSeries.Title = tag;
                    columnSeries.LabelPoint = point => $"{labels[(int)point.X]}: {point.Y.ToString()}";
                    seriesCollection.Add(columnSeries);
                }
            }
            AQIChart.Series = seriesCollection;
        }

        private async Task<string> FetchContentAsync(string url)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(200); // 設置超時時間為 200 秒
                try
                {
                    return await client.GetStringAsync(url);
                }
                catch (TaskCanceledException ex)
                {
                    MessageBox.Show("請求超時或被取消");
                    // 可以在這裡添加重試邏輯或其他錯誤處理
                    throw;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"讀取數據時發生錯誤: {ex.Message}");
                    throw;
                }
            }
        }

        private void RecordDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void RecordDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedRecords = RecordDataGrid.SelectedItems.Cast<Record>().ToList();
            statusTextBlock.Text = $"總共選取{selectedRecords.Count}筆記錄";
        }
    }
}
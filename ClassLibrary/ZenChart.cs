using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Windows.Forms.DataVisualization.Charting;
using System.Diagnostics;
using System.Security.Policy;
using System.ComponentModel.Design.Serialization;

namespace ZenForms.Core
{
    public enum ZenChartType { Line, Spline, Column, Bar, Area, Pie, Doughnut, StackedColumn}
    [ToolboxBitmap(typeof(Panel))]
    [Category("ZenFormsModernControls")]
    [Description("Modern chart types including line, column, bar, area, pie, and donut")]
    public class ZenChart : UserControl
    {
        private readonly Chart chart;
        private string chartTitle = "";
        private ZenChartType chartType = ZenChartType.Line;
        private bool showLegend = true;
        private bool showGrid = true;
        private bool showMarkers = true;
        private Color seriesColor = ZenTheme.Primary;
        private Color secondSeriesColor = ZenTheme.Accent;

        public ZenChart()
        {
            BackColor = ZenTheme.Surface;
            ForeColor = ZenTheme.Foreground;
            MinimumSize = new Size(220, 150);
            Size = new Size(480, 300);
            chart = new Chart { Dock = DockStyle.Fill, BackColor = ZenTheme.Surface, Palette = ChartColorPalette.None, AntiAliasing = AntiAliasingStyles.All, TextAntiAliasingQuality = TextAntiAliasingQuality.High };
            ChartArea area = new ChartArea("Main") { BackColor = ZenTheme.Surface };
            area.AxisX.LabelStyle.ForeColor = ZenTheme.Muted;
            area.AxisY.LabelStyle.ForeColor = ZenTheme.Muted;
            area.AxisX.LineColor = ZenTheme.Border;
            area.AxisY.LineColor = ZenTheme.Border;
            area.AxisX.MajorGrid.LineColor = ZenTheme.Border;
            area.AxisY.MajorGrid.LineColor = ZenTheme.Border;
            area.AxisX.MajorGrid.Enabled = true;
            area.AxisY.MajorGrid.Enabled = true;
            area.AxisX.MajorTickMark.Enabled = false;
            area.AxisY.MajorTickMark.Enabled = false;
            area.AxisX.LabelStyle.Font = new Font("Segoe UI", 8f);
            area.AxisY.LabelStyle.Font = new Font("Segoe UI", 8f);
            area.AxisX.IsMarginVisible = true;
            area.AxisY.IsStartedFromZero = true;
            chart.ChartAreas.Add(area);
            Legend legend = new Legend("Legend") { BackColor = ZenTheme.Surface, ForeColor = ZenTheme.Muted, Font = new Font("Segoe UI", 8f), Docking = Docking.Bottom };
            chart.Legends.Add(legend);
            chart.Titles.Add(new Title("", Docking.Top, new Font("Segoe UI Semibold", 11f), ZenTheme.Foreground));
            Controls.Add(chart);
            AddSeries("Serie 1");
            ApplyAppearance();
        }

        [Browsable(false)] public Chart InnerChart { get { return chart; } }

        [Category("ZenForms Chart"), DefaultValue("")]
        public string ChartTitle
        {
            get { return chartTitle; }
            set { chartTitle = value ?? ""; chart.Titles[0].Text = chartTitle; }
        }

        [Category("ZenForms Chart"), DefaultValue(ZenChartType.Line)]
        public ZenChartType ChartType
        {
            get { return chartType; }
            set { chartType = value; ApplyChartType(); }
        }

        [Category("ZenForms Chart"), DefaultValue(true)]
        public bool ShowLegend { get { return showLegend; } set { showLegend = value; ApplyAppearance(); } }

        [Category("ZenForms Chart"), DefaultValue(true)]
        public bool ShowGrid { get { return showGrid; } set { showGrid = value; ApplyAppearance(); } }

        [Category("ZenForms Chart"), DefaultValue(true)]
        public bool ShowMarkers { get { return showMarkers; } set { showMarkers = value; ApplyAppearance(); } }

        [Category("ZenForms Chart")] public Color SeriesColor { get { return seriesColor; } set { seriesColor = value; ApplyAppearance(); } }
        [Category("ZenForms Chart")] public Color SecondSeriesColor { get { return secondSeriesColor; } set { secondSeriesColor = value; ApplyAppearance(); } }

        public Series AddSeries(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentException("Enter with a name for this serie.", "name");
            Series series = new Series(name) { ChartArea = "Main", Legend = "Legend", ChartType = ToNativeType(chartType), BorderWidth = 3, Color = chart.Series.Count % 2 == 0 ? seriesColor : secondSeriesColor };
            chart.Series.Add(series);
            ApplyAppearance();
            return series;
        }

        public void AddPoint(string seriesName, string category, double value)
        {
            Series series = chart.Series.IndexOf(seriesName) >= 0 ? chart.Series[seriesName] : AddSeries(seriesName);
            series.Points.AddXY(category, value);
        }

        public void AddPoint(string seriesName, double value)
        {
            Series series = chart.Series.IndexOf(seriesName) >= 0 ? chart.Series[seriesName] : AddSeries(seriesName);
            series.Points.AddY(value);
        }

        public void ClearData()
        {
            foreach (Series series in chart.Series) series.Points.Clear();
        }

        public void ClearSeries()
        {
            chart.Series.Clear();
        }

        private void ApplyChartType()
        {
            foreach (Series series in chart.Series) series.ChartType = ToNativeType(chartType);
            ChartArea area = chart.ChartAreas[0];
            bool categorical = chartType == ZenChartType.Pie || chartType == ZenChartType.Doughnut;
            area.AxisX.Enabled = categorical ? AxisEnabled.False : AxisEnabled.Auto;
            area.AxisY.Enabled = categorical ? AxisEnabled.False : AxisEnabled.Auto;
            ApplyAppearance();
        }

        private void ApplyAppearance()
        {
            if (chart == null) return;
            chart.BackColor = BackColor;
            chart.Legends[0].Enabled = showLegend && chartType != ZenChartType.Pie && chartType != ZenChartType.Doughnut;
            ChartArea area = chart.ChartAreas[0];
            area.AxisX.MajorGrid.Enabled = showGrid;
            area.AxisY.MajorGrid.Enabled = showGrid;
            for (int i = 0; i < chart.Series.Count; i++)
            {
                Series series = chart.Series[i];
                series.Color = i % 2 == 0 ? seriesColor : secondSeriesColor;
                series.BorderWidth = 3;
                series.MarkerStyle = showMarkers && (chartType == ZenChartType.Line || chartType == ZenChartType.Spline) ? MarkerStyle.Circle : MarkerStyle.None;
                series.MarkerSize = 7;
                series.MarkerColor = series.Color;
                series.IsValueShownAsLabel = chartType == ZenChartType.Pie || chartType == ZenChartType.Doughnut;
                series.LabelForeColor = ZenTheme.Foreground;
            }
        }

        private static SeriesChartType ToNativeType(ZenChartType type)
        {
            switch (type)
            {
                case ZenChartType.Spline: return SeriesChartType.Spline;
                case ZenChartType.Column: return SeriesChartType.Column;
                case ZenChartType.Bar: return SeriesChartType.Bar;
                case ZenChartType.Area: return SeriesChartType.Area;
                case ZenChartType.Pie: return SeriesChartType.Pie;
                case ZenChartType.Doughnut: return SeriesChartType.Doughnut;
                case ZenChartType.StackedColumn: return SeriesChartType.StackedColumn;
                default: return SeriesChartType.Line;
            }
        }
    }
}

// Watashi wa watashi sore dake


#region Using declarations
using System;
using System.Windows;
using NinjaTrader.Core;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.AddOns.ChartDataLogger
{
    public class ChartDataLoggerAddOn : AddOnBase
    {
        private const string MENU_HEADER = "📊 Chart Data Logger";
        private NTMenuItem menuItem;
        private NTMenuItem existingMenuItemInControlCenter;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name        = "ChartDataLoggerAddOn";
                Description = "Direct historical OHLCV data export to CSV format.";
            }
            else if (State == State.Configure)
            {
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (Window w in Application.Current.Windows)
                        {
                            if (w is ControlCenter cc)
                                AttachToControlCenter(cc);
                        }
                    });
                }
            }
        }

        protected override void OnWindowCreated(Window window)
        {
            if (window is ControlCenter cc)
                AttachToControlCenter(cc);
        }

        private void AttachToControlCenter(ControlCenter cc)
        {
            if (cc == null) return;

            existingMenuItemInControlCenter = cc.FindFirst("ControlCenterMenuItemNew") as NTMenuItem;
            if (existingMenuItemInControlCenter == null) return;

            // Avoid duplicates when assembly reloads
            foreach (var item in existingMenuItemInControlCenter.Items)
            {
                if (item is NTMenuItem mi && mi.Header != null && mi.Header.ToString() == MENU_HEADER)
                    return;
            }

            menuItem = new NTMenuItem
            {
                Header = MENU_HEADER,
                Style  = Application.Current.TryFindResource("MainMenuItem") as Style
            };

            existingMenuItemInControlCenter.Items.Add(menuItem);
            menuItem.Click += OnMenuItemClick;
        }

        protected override void OnWindowDestroyed(Window window)
        {
            if (menuItem != null && window is ControlCenter)
            {
                if (existingMenuItemInControlCenter != null && existingMenuItemInControlCenter.Items.Contains(menuItem))
                    existingMenuItemInControlCenter.Items.Remove(menuItem);

                menuItem.Click -= OnMenuItemClick;
                menuItem = null;
            }
        }

        private void OnMenuItemClick(object sender, RoutedEventArgs e)
        {
            Globals.RandomDispatcher.BeginInvoke(new Action(() =>
            {
                new ChartDataLoggerWindow().Show();
            }));
        }
    }
}

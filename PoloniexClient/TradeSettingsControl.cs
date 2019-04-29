﻿using CryptoMarketClient.Common;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraLayout.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CryptoMarketClient {
    public partial class TradeSettingsControl : XtraUserControl {
        public TradeSettingsControl() {
            InitializeComponent();
        }

        bool showTrailingSettings = true;
        public bool ShowTrailingSettings {
            get { return showTrailingSettings; }
            set {
                if(ShowTrailingSettings == value)
                    return;
                this.showTrailingSettings = value;
            }
        }

        public ITradingResultOperationsProvider OperationsProvider { get; set; }

        public Ticker Ticker { get; set; }
        TradingSettings settings;
        public TradingSettings Settings {
            get {
                return settings;
            }
            set {
                if(Settings == value)
                    return;
                this.settings = value;
                OnSettingsChanged();
            }
        }
        void OnSettingsChanged() {
            this.tralingSettingsBindingSource.DataSource = Settings;
            if(Settings.Ticker != null) {
                itemForSpendBTC.Text = "Total " + Settings.Ticker.BaseCurrency;
            }
        }

        protected bool ValidateChildrenCore() {
            return true;
        }

        private void MakeTrade(OrderType type) {
            if(!ValidateChildrenCore()) {
                XtraMessageBox.Show("Not all fields are filled!");
                return;
            }
            
            string validationError = Ticker.ValidateTrade(Settings.TradePrice, Settings.Amount);
            if(!string.IsNullOrEmpty(validationError)) {
                XtraMessageBox.Show("Error validating trade. Values will be corrected. Error was: " + validationError);

                double rate = Settings.TradePrice, amount = Settings.Amount;
                Ticker.CorrectTrade(ref rate, ref amount);

                Settings.TradePrice = rate;
                Settings.Amount = amount;
                return;
            }
            if(type == OrderType.Buy) {
                if(Ticker.Buy(Settings.TradePrice, Settings.Amount) == null) {
                    XtraMessageBox.Show("Error buying. Please try later again. Last Error: " + LogManager.Default.Messages.Last().Text);
                    return;
                }
            }
            else {
                Settings.Enabled = false;
                if(Ticker.Sell(Settings.TradePrice, Settings.Amount) == null) {
                    XtraMessageBox.Show("Error selling. Please try later again." + LogManager.Default.Messages.Last().Text);
                    return;
                }
            }
            if(Settings.Enabled) {
                Settings.Date = DateTime.UtcNow;
                Ticker.Trailings.Add(Settings);
                Settings.Start();
                if(OperationsProvider != null)
                    OperationsProvider.ShowTradingResult(Ticker);
                Ticker.Save();

                XtraMessageBox.Show("Trailing added!");
            }
        }

        private void OnBuyButtonClick(object sender, EventArgs e) {
            MakeTrade(OrderType.Buy);
        }
        public void SelectedAskChanged(object sender, FocusedRowChangedEventArgs e) {
            OrderBookEntry entry = (OrderBookEntry)((GridView)sender).GetRow(e.FocusedRowHandle);
            BuyPriceTextEdit.EditValue = entry.Value;
        }

        private void checkEdit1_CheckedChanged(object sender, EventArgs e) {
            ItemForTakeProfitPercent.Enabled = ItemForTakeProfitStartPercent.Enabled = ItemForIngoreStopLoss.Enabled = this.checkEdit1.IsOn;
            ItemForStopLossPricePercent.Enabled = ItemForIncrementalStopLoss.Enabled = this.checkEdit1.IsOn && !this.ceIgnoreStopLoss.IsOn;
        }

        private void ceIgnoreStopLoss_CheckedChanged(object sender, EventArgs e) {
            ItemForIncrementalStopLoss.Enabled = ItemForStopLossPricePercent.Enabled = !this.ceIgnoreStopLoss.IsOn && this.checkEdit1.IsOn;
        }

        private void btnSell_Click(object sender, EventArgs e) {
            MakeTrade(OrderType.Sell);
        }

        private void tbDepositPercent_EditValueChanged(object sender, EventArgs e) {
            if(Settings.OrderPrice == 0)
                Settings.TradePrice = Ticker.OrderBook.Bids[0].Value;
            Settings.Amount = tbDepositPercent.Value / 100.0 * Ticker.BaseCurrencyBalance / Settings.TradePrice;
            this.layoutControlItem4.Text = tbDepositPercent.Value + "% of Deposit";
        }
    }

    public interface ITradingResultOperationsProvider {
        void ShowTradingResult(Ticker ticker);
    }
}

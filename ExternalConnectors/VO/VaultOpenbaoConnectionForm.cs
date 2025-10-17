namespace ExternalConnectors.VO
{
    public partial class VaultOpenbaoConnectionForm : Form {
        public VaultOpenbaoConnectionForm() {
            InitializeComponent();

        }

        private void VaultOpenbaoConnectionForm_Activated(object sender, EventArgs e) {
            tbUrl.Focus();
        }
    }
}

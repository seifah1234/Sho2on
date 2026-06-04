using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows; using HR_Application.Helpers;

namespace HR_Application
{
    internal class LoadingBar
    {
        System.Windows.Controls.Image loadingProgressBar = new System.Windows.Controls.Image();

        public LoadingBar(System.Windows.Controls.Image image)
        {
            this.loadingProgressBar = image;
        }

        public void ShowLoadingIndicator()
        {
            loadingProgressBar.Width = 50; // Or your desired width
            loadingProgressBar.Height = 50; // Or your desired height
            loadingProgressBar.Visibility = Visibility.Visible; // Show loading indicator
        }

        public void HideLoadingIndicator()
        {
            loadingProgressBar.Width = 0;
            loadingProgressBar.Height = 0;
            loadingProgressBar.Visibility = Visibility.Collapsed; // Hide loading indicator
        }
    }
}

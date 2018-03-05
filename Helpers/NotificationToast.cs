using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace CampusNet
{
    public class NotificationToast
    {
        private static string logo = "/Assets/Store/StoreLogo.scale-400.png";
        private ToastNotification toast;

        public NotificationToast(string title, string content)
        {
            ToastVisual visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText()
                        {
                            Text = title
                        },
                        new AdaptiveText()
                        {
                            Text = content
                        }
                    },
                    AppLogoOverride = new ToastGenericAppLogo()
                    {
                        Source = logo,
                        HintCrop = ToastGenericAppLogoCrop.Circle
                    }
                }
            };

            ToastContent toastContent = new ToastContent()
            {
                Visual = visual
            };

            toast = new ToastNotification(toastContent.GetXml());
        }

        public string Tag { get => toast.Tag; set => toast.Tag = value; }
        public string Group { get => toast.Group; set => toast.Group = value; }

        public void Show()
        {
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}

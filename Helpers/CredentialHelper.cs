using System.Linq;
using Windows.Security.Credentials;

namespace CampusNet
{
    class CredentialHelper
    {
        public static void AddAccount(string username, string password)
        {
            var vault = new PasswordVault();

            var credential = GetCredentialFromLocker(username);
            if (credential == null)
            {
                vault.Add(new PasswordCredential("CampusNet", username, password));
            }
            else
            {
                RemoveAccount(username);
                vault.Add(new PasswordCredential("CampusNet", username, password));
            }
        }

        public static bool RemoveAccount(string username)
        {
            var vault = new PasswordVault();

            try
            {
                var credentialList = vault.FindAllByUserName(username);
                if (credentialList.Count > 0)
                {
                    var credential = credentialList.First();
                    credential.RetrievePassword();
                    vault.Remove(new PasswordCredential("CampusNet", credential.UserName, credential.Password));
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static PasswordCredential GetCredentialFromLocker(string username)
        {
            var vault = new PasswordVault();

            try
            {
                var credentialList = vault.FindAllByUserName(username);
                if (credentialList.Count > 0)
                {
                    return credentialList.First();
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}

using System;

namespace libal.Services
{
    public class EmailFallbackGenerator
    {

        public static string email(string email, string companyName, string defaultName)
        {
            var isEmailNotEmpty = email != null && email.Length > 0 && !"n/a".Equals(email);
            var isCompanyNameNotEmpty = companyName != null && companyName.Length > 0 && !"n/a".Equals(companyName);
            var isDefaultNameNotEmpty = defaultName != null && defaultName.Length > 0 && !"n/a".Equals(defaultName);

            string resultEmail;
            
            if (isEmailNotEmpty && email.Contains("@"))
            {
                resultEmail = email.ToLower().Replace(" ", ".");
            }
            else if (isEmailNotEmpty && !email.Contains("@"))
            {
                resultEmail = "invalid@" + email.ToLower().Replace(" ", ".") + ".com";
            }
            else if (isCompanyNameNotEmpty)
            {
                resultEmail = "invalid@" + companyName.ToLower().Replace(" ", ".") + ".com";
            }
            else if (isDefaultNameNotEmpty)
            {
                resultEmail = "invalid@" + defaultName.ToLower().Replace(" ", ".") + ".com";
            }
            else
            {
                resultEmail = Guid.NewGuid().ToString() + "@unknown.com";
            }

            return resultEmail;
        }

    }
}

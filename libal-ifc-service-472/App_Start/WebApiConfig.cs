using System.Web.Http;

namespace libal_ifc_service_472
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web-API-Konfiguration und -Dienste

            // Web-API-Routen
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "LIBAL IFC API",
                routeTemplate: "api/{controller}/{action}/{uuid}",
                defaults: new { action = RouteParameter.Optional, uuid = RouteParameter.Optional }
            );
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using EO.Serwis.Portal.DataAccess.Contract.POCO;
using EO.Serwis.Portal.DataAccess.Contract.Repositories;
using EO.Serwis.Portal.WebApi.Core3.Helpers;
using EO.Serwis.Portal.WebApi.Core3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace EO.Serwis.Portal.WebApi.COre3.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        public IParametrySystemuRepository ParametrySystemuRepository { get; }
        public IUzytkownicyRepository UzytkownicyRepository { get; }
        public IEmailRepository EmailRepository { get; }
        public IConfiguration Conf { get; }

        public AccountController(IParametrySystemuRepository parametrySystemuRepository, IUzytkownicyRepository uzytkownicyRepository, IEmailRepository emailRepository, IConfiguration conf)
        {
            ParametrySystemuRepository = parametrySystemuRepository;
            UzytkownicyRepository = uzytkownicyRepository;
            EmailRepository = emailRepository;
            Conf = conf;
        }

        [HttpPost]
        [ActionName("InitializePasswordChange")]
        public IActionResult InitializePasswordChange([FromBody] dynamic model)
        {
            try
            {
                Log.Information($"Getting user by login: {model?.Login}.");
                var url = Conf.GetSection("UserSecrets")["Url"];
                var login = (string)model.Login;
                var user = UzytkownicyRepository.Select(p => p.Login == login, c => c.SesjeUzytkownika).SingleOrDefault();
                if (user == null)
                {
                    Log.Error("Uzytkownik o podanym loginie nie został odnaleziony w bazie.");
                    return BadRequest("Uzytkownik o podanym loginie nie został odnaleziony w bazie.");//jeśli nie istnieje to error               
                }
                var guid = Guid.NewGuid().ToString();
                var sessionId = guid.Substring(0, 5) + guid.Substring(28, 5);
                var parametry = ParametrySystemuRepository.Select(p => p.KategoriaParametru == "Email" && p.KategoriaIIParmetru == "PortalWyceny_ZmianaHasla").ToList();

                if (user.SesjeUzytkownika.Count > 0)
                {
                    user.SesjeUzytkownika.RemoveAll(p => p.UzytkownikPortaluId == user.Id);
                }
                user.SesjeUzytkownika.Add(new UzytkownicyPortaluSesjePOCO()
                {
                    DataWaznosciSesjiZmianyHasla = DateTime.Now.AddDays(1),
                    Id = sessionId
                });

                UzytkownicyRepository.Modify(user);
                UzytkownicyRepository.Save();

                Log.Information($"Dodano nową sesję zmiany hasła dla loginu : {model?.Login}.");

                EmailRepository.Add(new EmailPOCO()
                {
                    Status = "Oczekuje na wysłanie",
                    Temat = parametry.Single(p => p.NazwaParametru == "EmailTemat").WartoscParametru.Replace("<UserPortal>", $"{user.Imie} {user.Nazwisko}"),
                    OdAddress = "serwis@eo.pl",
                    OdDisplayName = "Serwis EO Networks",
                    DoAddress = ((string)model.Login).Trim(),
                    DoDisplayName = $"{user.Imie} {user.Nazwisko}",
                    MessageiD = Guid.NewGuid().ToString(),
                    Header_Thread_Index = Guid.NewGuid().ToString(),
                    Priorytet = 0,
                    EmailPrzyjety = false,
                    Format = "html",
                    Wiadomosc = parametry.Single(p => p.NazwaParametru == "EmailTresc").WartoscParametru.Replace("<Link>", $"{url}/account/changepassword/{sessionId}").Replace("<UserPortal>", $"{user.Imie} {user.Nazwisko}"),
                    DataWyslania = DateTime.Now,
                    DataOdbioru = DateTime.Now,
                    WiadomoscPrzetworzona = 0
                });
                EmailRepository.Save();

                Log.Information($"Pod adress {model?.Login} został wysłany email z linkiem do zmiany hasła.");

                return Ok();
            }

            catch (Exception ex)
            {
                Log.Fatal(ex.ToString());
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [ActionName("ChangePassword")]
        public IActionResult ChangePassword([FromBody] dynamic model)
        {
            try
            {
                Log.Information($"Changing password for session: {model?.SessionId}.");
                var sessionId = (string)model.SessionId;
                var user = UzytkownicyRepository.Select(p => p.SesjeUzytkownika.Select(d => d.Id).Contains(sessionId), c => c.SesjeUzytkownika).SingleOrDefault();
                if (user == null)
                {
                    Log.Error($"Nie odnaleziono sesji o identyfikatorze {(string)model.SessionId} w bazie danych.");
                    return BadRequest($"Nie odnaleziono sesji o identyfikatorze {(string)model.SessionId} w bazie danych.");//jeśli nie istnieje to error               
                }

                user.Password = HashHelper.CalculateSHA2Hash(user.RowUID + model?.Password).ToLower();
                UzytkownicyRepository.Modify(user);
                UzytkownicyRepository.Save();

                return Ok();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex.ToString());
                return BadRequest(ex.Message);
            }
        }
    }
}
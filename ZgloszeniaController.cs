using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EO.Serwis.Portal.DataAccess.Contract.Repositories;
using EO.Serwis.Portal.DataAccess.Repositories;
using EO.Serwis.Portal.WebApi.Core3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;

namespace EO.Serwis.Portal.WebApi.Core3.Controllers
{
    //[Route("api/[controller]/[action]")]
    [ApiController]
    public class ZgloszeniaController : ControllerBase
    {
        public IWycenyRepository WycenyRepo { get; }
        public IZgloszenieRepository ZgloszeniaRepo { get; }

        public ZgloszeniaController(IWycenyRepository wycenyRepo, IZgloszenieRepository zgloszeniaRepo)
        {
            WycenyRepo = wycenyRepo;
            ZgloszeniaRepo = zgloszeniaRepo;
        }

        [HttpGet]
        [Route("api/Zgloszenia")]
        public IActionResult Get([FromQuery] long idUzytkownika)
        {
            var list = WycenyRepo.GetWyceny(idUzytkownika);
            return Ok(list.Select(c =>
                new
                {
                    IdZgloszenia = c.IdZgloszenia,
                    IdWyceny = c.IdWyceny,
                    Opis = c.Zgloszenie.OpisSzczeglowy,
                    UrlWyceny = c.UrlWyceny
                }
                ).ToList());
        }

        [HttpGet]
        [Route("api/Zgloszenia/DaneUzytkownika")]
        public IActionResult GetCustomerData([FromQuery] long idZgloszenia)
        {
            try
            {
                var zgloszenie = ZgloszeniaRepo.FindEntity(idZgloszenia);

                //var wycena = WycenyRepo.Select(p => p.IdWyceny == zgloszenie.);

                if (zgloszenie == null)
                {
                    return NotFound();
                }

                return Ok(new CustomerDataModel()
                {
                    IdZgloszenia = zgloszenie.IdZgloszenia,
                    Imie = zgloszenie.UzytkownikImie,
                    Nazwisko = zgloszenie.UyztkownikNazwisko,
                    Adres = zgloszenie.UzytkownikAdres,
                    KodPocztowy = zgloszenie.UzytkownikKodPocztowy,
                    Miasto = zgloszenie.UzytkownikMiasto,
                    Mail = zgloszenie.UzytkownikEmail,
                    TelefonKomorkowy = zgloszenie.UzytkownikTelefonKomorkowy,
                    WyborRealizatora = zgloszenie.WyborRealizatora
                });
            }
            catch (Exception ex)
            {
                Log.Fatal(ex.ToString());
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("api/zgloszenia/update")]
        public IActionResult UpdateServiceOrder([FromBody] CustomerDataModel model)
        {
            var zgloszenie = ZgloszeniaRepo.FindEntity(model.IdZgloszenia);

            if (zgloszenie == null)
            {
                return NotFound();
            }
            {
                zgloszenie.IdZgloszenia = model.IdZgloszenia;
                zgloszenie.UzytkownikImie = model.Imie;
                zgloszenie.UyztkownikNazwisko = model.Nazwisko;
                zgloszenie.UzytkownikMiasto = model.Miasto;
                zgloszenie.UzytkownikAdres = model.Adres;
                zgloszenie.UzytkownikKodPocztowy = model.KodPocztowy;
                zgloszenie.UzytkownikEmail = model.Mail;
                zgloszenie.UzytkownikTelefonKomorkowy = model.TelefonKomorkowy;
            }

            ZgloszeniaRepo.Modify(zgloszenie);
            ZgloszeniaRepo.Save();

            return Ok();
        }

    }
}
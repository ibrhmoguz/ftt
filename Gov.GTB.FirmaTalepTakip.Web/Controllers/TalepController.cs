﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using AutoMapper;
using Gov.GTB.FirmaTalepTakip.Model.Entities;
using Gov.GTB.FirmaTalepTakip.Model.Enums;
using Gov.GTB.FirmaTalepTakip.Model.ViewModel;
using Gov.GTB.FirmaTalepTakip.Repository.Interface;
using Gov.GTB.FirmaTalepTakip.Repository.Repository;
using Gov.GTB.FirmaTalepTakip.Web.Helpers;
using Gov.GTB.FirmaTalepTakip.Web.Infrastructure.Concrete;

namespace Gov.GTB.FirmaTalepTakip.Web.Controllers
{
    [Authorize]
    [SessionExpireFilter]
    public class TalepController : Controller
    {
        private readonly ITalepDetayFirmaRepository _talepDetayFirmaRepository;
        private readonly IRefTalepKonuRepository _refTalepKonuRepository;
        public readonly CevapRepository _cevapRepository;

        public TalepController(ITalepDetayFirmaRepository talepDetayFirmaRepository, IRefTalepKonuRepository refTalepKonuRepository, CevapRepository cevapRepository)
        {
            _talepDetayFirmaRepository = talepDetayFirmaRepository;
            _refTalepKonuRepository = refTalepKonuRepository;
            _cevapRepository = cevapRepository;
        }

        public ActionResult Liste()
        {
            var talepler = TalepleriGetir();
            var talepViewModel = Mapper.Map<IEnumerable<TalepDetayFirma>, IEnumerable<TalepDetayFirmaViewModel>>(talepler);
            var siraNo = 1;
            foreach (var talepDetayFirmaViewModel in talepViewModel)
            {
                talepDetayFirmaViewModel.SiraNo = siraNo++;
            }
            return View(talepViewModel);
        }

        private IEnumerable<TalepDetayFirma> TalepleriGetir()
        {
            var currentUserTcNo = (long)Session["CurrentUserTcNo"];
            var kullaniciYetkileri = (KullaniciYetkileri)Session["CurrentUser_Auths"];
            string bolgeKodu = null;
            if (kullaniciYetkileri.KullaniciRolEnum == RolEnum.BIP)
            {
                var bipGumrukKullanici = (GumrukKullanici)Session["CurrentGumrukKullanici"];
                bolgeKodu = bipGumrukKullanici.BolgeKodu;
            }

            return _talepDetayFirmaRepository.TalepListesi(currentUserTcNo, bolgeKodu);
        }

        public ActionResult Ekle()
        {
            var talepViewModel = new TalepDetayFirmaViewModel()
            {
                Konular = _refTalepKonuRepository.TalepKonuListesi()
            };
            return View("Duzenle", talepViewModel);
        }

        public ActionResult Duzenle(int id)
        {
            var talepFromDb = _talepDetayFirmaRepository.TalepDetayGetir(id);
            var talepViewModel = Mapper.Map<TalepDetayFirma, TalepDetayFirmaViewModel>(talepFromDb);
            talepViewModel.Konular = _refTalepKonuRepository.TalepKonuListesi();
            return View(talepViewModel);
        }

        public ActionResult Ara(string talepReferansNo)
        {
            var talepler = TalepleriGetir();
            var talepViewModel = Mapper.Map<IEnumerable<TalepDetayFirma>, IEnumerable<TalepDetayFirmaViewModel>>(talepler);

            if (string.IsNullOrEmpty(talepReferansNo))
            {
                ModelState.AddModelError("talepReferansNo", Resources.TalepReferansNoEmptyErrorMsg);
                return View("Liste", talepViewModel);
            }

            long talepReferansNoParam = 0;
            if (!long.TryParse(talepReferansNo, out talepReferansNoParam))
            {
                ModelState.AddModelError("talepReferansNo", Resources.TalepReferansNoFormatErrorMsg);
                return View("Liste", talepViewModel);
            }

            var filteredTalepler = talepViewModel.Where(z => z.TalepReferansNo == talepReferansNoParam);
            return View("Liste", filteredTalepler);
        }

        [HttpPost]
        public ActionResult Duzenle(TalepDetayFirmaViewModel talepDetayFirmaViewModel)
        {
            if (ModelState.IsValid)
            {
                var talep = Mapper.Map<TalepDetayFirmaViewModel, TalepDetayFirma>(talepDetayFirmaViewModel);
                var firmaKullanici = (FirmaKullanici)Session["CurrentFirmaKullanici"];
                talep.VergiNo = firmaKullanici.VergiNo;
                talep.FirmaKullaniciId = firmaKullanici.Id;
                talep.TalepTarih = DateTime.Now;
                talep.CevapDurum = false;
                _talepDetayFirmaRepository.TalepKaydetGuncelle(talep);
                return RedirectToAction("Liste");
            }
            else
            {
                var talepViewModel = new TalepDetayFirmaViewModel()
                {
                    Konular = _refTalepKonuRepository.TalepKonuListesi()
                };
                return View("Duzenle", talepViewModel);
            }
        }

        public ActionResult Cevap(long id)
        {
            var cevapTalep = _cevapRepository.TalepCevabiGetir(id);
            var cevapViewModel = Mapper.Map<CevapDetayGumruk, CevapViewModel>(cevapTalep);
            var talep = _talepDetayFirmaRepository.TalepDetayGetir(id);
            cevapViewModel.TalepReferansNo = talep.TalepReferansNo;
            return View(cevapViewModel);
        }
    }
}
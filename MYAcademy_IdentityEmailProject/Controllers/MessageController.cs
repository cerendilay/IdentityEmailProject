using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MYAcademy_IdentityEmailProject.Context;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Services;
using System.Net;
using System.Text.RegularExpressions;

namespace MYAcademy_IdentityEmailProject.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly MailService _mailService;

        public MessageController(EmailContext context, UserManager<AppUser> userManager, MailService mailService)
        {
            _context = context;
            _userManager = userManager;
            _mailService = mailService;
        }

        // Oturum açan kullanıcının e-posta adresi (tüm sorgular buna göre filtrelenir)
        private async Task<string> GetUserEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user!.Email!;
        }

        // ---------------------------------------------------------------- Gelen Kutusu
        public async Task<IActionResult> Inbox(string? q, int? categoryId, string sort = "desc")
        {
            var email = await GetUserEmailAsync();

            var query = _context.Messages
                .Include(x => x.Category)
                .Where(x => x.ReceiverEmail == email && !x.IsDraft && !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(x => x.Subject.Contains(q) || x.MessageDetail.Contains(q) || x.SenderEmail.Contains(q));
            }

            if (categoryId != null)
            {
                query = query.Where(x => x.CategoryId == categoryId);
            }

            query = sort == "asc"
                ? query.OrderBy(x => x.SendDate)
                : query.OrderByDescending(x => x.SendDate);

            ViewBag.Q = q;
            ViewBag.CategoryId = categoryId;
            ViewBag.Sort = sort;
            ViewBag.Categories = new SelectList(_context.Categories.ToList(), "CategoryId", "CategoryName", categoryId);

            var values = await query.ToListAsync();
            return View(values);
        }

        // ---------------------------------------------------------------- Mesaj Detayı
        public async Task<IActionResult> MessageDetail(int id)
        {
            var email = await GetUserEmailAsync();

            var message = await _context.Messages.Include(x => x.Category).FirstOrDefaultAsync(x => x.MessageId == id);

            if (message == null)
                return NotFound();

            // Başkasına ait mesaj URL'den açılamaz
            if (message.ReceiverEmail != email && message.SenderEmail != email)
                return Forbid();

            // Taslaklar detayda değil düzenleme ekranında açılır
            if (message.IsDraft)
                return RedirectToAction("CreateMessage", new { id = message.MessageId });

            if (message.ReceiverEmail == email && !message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            ViewBag.IsReceiver = message.ReceiverEmail == email;
            return View(message);
        }

        // ---------------------------------------------------------------- Yeni Mesaj / Taslak düzenle / Yanıtla / İlet
        public async Task<IActionResult> CreateMessage(int? id, int? replyId, int? forwardId)
        {
            var email = await GetUserEmailAsync();
            Message? model = null;

            if (id != null)
            {
                // Taslağı düzenle
                var draft = await _context.Messages.FindAsync(id);
                if (draft == null) return NotFound();
                if (draft.SenderEmail != email || !draft.IsDraft) return Forbid();
                model = draft;
            }
            else if (replyId != null || forwardId != null)
            {
                var original = await _context.Messages.FindAsync(replyId ?? forwardId);
                if (original == null) return NotFound();
                if (original.ReceiverEmail != email && original.SenderEmail != email) return Forbid();

                var quote = $"<p><br></p><p>--- {original.SendDate:dd.MM.yyyy HH:mm} tarihinde {WebUtility.HtmlEncode(original.SenderEmail)} yazdı ---</p>" +
                            $"<blockquote>{WebUtility.HtmlEncode(PlainText(original.MessageDetail))}</blockquote>";

                if (replyId != null)
                {
                    model = new Message
                    {
                        ReceiverEmail = original.SenderEmail == email ? original.ReceiverEmail : original.SenderEmail,
                        Subject = original.Subject.StartsWith("RE:") ? original.Subject : "RE: " + original.Subject,
                        CategoryId = original.CategoryId,
                        MessageDetail = quote
                    };
                }
                else
                {
                    model = new Message
                    {
                        ReceiverEmail = "",
                        Subject = original.Subject.StartsWith("FW:") ? original.Subject : "FW: " + original.Subject,
                        CategoryId = original.CategoryId,
                        MessageDetail = quote
                    };
                }
            }

            ViewBag.Categories = new SelectList(_context.Categories.ToList(), "CategoryId", "CategoryName");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMessage(Message message, string? submitType)
        {
            var email = await GetUserEmailAsync();
            bool isDraft = submitType == "draft";

            // Entity'deki zorunlu alanlar (SenderEmail vb.) formdan gelmediği için kendi doğrulamamızı yapıyoruz
            ModelState.Clear();

            message.ReceiverEmail = message.ReceiverEmail?.Trim() ?? "";
            message.Subject = message.Subject?.Trim() ?? "";
            message.MessageDetail = message.MessageDetail ?? "";

            if (message.CategoryId != null && !_context.Categories.Any(x => x.CategoryId == message.CategoryId))
                ModelState.AddModelError(nameof(Message.CategoryId), "Geçersiz kategori.");

            if (!isDraft)
            {
                if (string.IsNullOrEmpty(message.ReceiverEmail))
                    ModelState.AddModelError(nameof(Message.ReceiverEmail), "Alıcı e-posta adresini girin.");
                else if (!IsValidEmail(message.ReceiverEmail))
                    ModelState.AddModelError(nameof(Message.ReceiverEmail), "Geçerli bir e-posta adresi girin.");

                if (message.CategoryId == null)
                    ModelState.AddModelError(nameof(Message.CategoryId), "Lütfen bir kategori seçin.");

                if (string.IsNullOrEmpty(message.Subject))
                    ModelState.AddModelError(nameof(Message.Subject), "Konu alanı boş bırakılamaz.");
                else if (message.Subject.Length > 200)
                    ModelState.AddModelError(nameof(Message.Subject), "Konu en fazla 200 karakter olabilir.");

                if (string.IsNullOrEmpty(PlainText(message.MessageDetail)))
                    ModelState.AddModelError(nameof(Message.MessageDetail), "Mesaj içeriği boş olamaz.");
            }
            else if (string.IsNullOrEmpty(message.ReceiverEmail) && string.IsNullOrEmpty(message.Subject) && string.IsNullOrEmpty(PlainText(message.MessageDetail)))
            {
                ModelState.AddModelError("", "Boş bir mesaj taslak olarak kaydedilemez.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(_context.Categories.ToList(), "CategoryId", "CategoryName");
                return View(message);
            }

            Message entity;
            if (message.MessageId > 0)
            {
                // Var olan taslak güncelleniyor / gönderiliyor → sadece kendi taslağı olabilir
                var draft = await _context.Messages.FindAsync(message.MessageId);
                if (draft == null) return NotFound();
                if (draft.SenderEmail != email || !draft.IsDraft) return Forbid();
                entity = draft;
            }
            else
            {
                entity = new Message();
                _context.Messages.Add(entity);
            }

            entity.SenderEmail = email;   // gönderen bilgisi Identity hesabından
            entity.ReceiverEmail = message.ReceiverEmail;
            entity.Subject = message.Subject;
            entity.MessageDetail = message.MessageDetail;
            entity.CategoryId = message.CategoryId;
            entity.SendDate = DateTime.Now;
            entity.IsDraft = isDraft;
            entity.IsRead = false;
            entity.IsFavorite = false;
            entity.IsDeleted = false;

            if (!isDraft)
            {
                // Alıcı sistemde kayıtlı değilse gerçek e-posta olarak SMTP ile gönder
                var receiverUser = await _userManager.FindByEmailAsync(message.ReceiverEmail);
                if (receiverUser == null)
                {
                    try
                    {
                        _mailService.SendMail(message.ReceiverEmail, message.Subject,
                            message.MessageDetail + $"<hr/><small>Bu mesaj Mailora üzerinden {WebUtility.HtmlEncode(email)} tarafından gönderildi.</small>",
                            isHtml: true);
                    }
                    catch
                    {
                        ModelState.AddModelError("", "E-posta SMTP ile gönderilemedi. Lütfen daha sonra tekrar deneyin.");
                        ViewBag.Categories = new SelectList(_context.Categories.ToList(), "CategoryId", "CategoryName");
                        return View(message);
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = isDraft ? "Mesaj taslak olarak kaydedildi." : "Mesajınız gönderildi.";
            return isDraft ? RedirectToAction("Drafts") : RedirectToAction("Sendbox");
        }

        // ---------------------------------------------------------------- Listeler
        public async Task<IActionResult> Sendbox()
        {
            var email = await GetUserEmailAsync();
            var values = await _context.Messages.Include(x => x.Category)
                .Where(x => x.SenderEmail == email && !x.IsDraft)
                .OrderByDescending(x => x.SendDate).ToListAsync();
            return View(values);
        }

        public async Task<IActionResult> Starred()
        {
            var email = await GetUserEmailAsync();
            var values = await _context.Messages.Include(x => x.Category)
                .Where(x => x.ReceiverEmail == email && x.IsFavorite && !x.IsDraft && !x.IsDeleted)
                .OrderByDescending(x => x.SendDate).ToListAsync();
            return View(values);
        }

        public async Task<IActionResult> Drafts()
        {
            var email = await GetUserEmailAsync();
            var values = await _context.Messages.Include(x => x.Category)
                .Where(x => x.SenderEmail == email && x.IsDraft)
                .OrderByDescending(x => x.SendDate).ToListAsync();
            return View(values);
        }

        public async Task<IActionResult> Trash()
        {
            var email = await GetUserEmailAsync();
            var values = await _context.Messages.Include(x => x.Category)
                .Where(x => x.ReceiverEmail == email && x.IsDeleted && !x.IsDraft)
                .OrderByDescending(x => x.SendDate).ToListAsync();
            return View(values);
        }

        // ---------------------------------------------------------------- Yıldız / okundu / çöp kutusu işlemleri
        // Not: Tüm işlemler sadece ReceiverEmail == oturum açan kullanıcı olan mesajlarda çalışır.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStar(int id, bool? starred)
        {
            var message = await GetOwnReceivedMessageAsync(id);
            if (message == null) return NotFound();

            message.IsFavorite = starred ?? !message.IsFavorite;
            await _context.SaveChangesAsync();
            return BackOrOk();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int[] ids)
        {
            await UpdateOwnMessagesAsync(ids, x => x.IsRead = true);
            return BackOrOk();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsUnread(int[] ids)
        {
            await UpdateOwnMessagesAsync(ids, x => x.IsRead = false);
            if (IsAjax()) return Ok();
            return RedirectToAction("Inbox");
        }

        // Çöp kutusuna taşı
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int[] ids)
        {
            await UpdateOwnMessagesAsync(ids, x => x.IsDeleted = true);
            if (IsAjax()) return Ok();
            TempData["Success"] = "Mesaj çöp kutusuna taşındı.";
            return RedirectToAction("Inbox");
        }

        // Çöp kutusundan geri al
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var message = await GetOwnReceivedMessageAsync(id);
            if (message == null) return NotFound();

            message.IsDeleted = false;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Mesaj gelen kutusuna geri alındı.";
            return RedirectToAction("Trash");
        }

        // Kalıcı olarak sil (çöp kutusundaki mesaj veya taslak)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PermanentDelete(int id)
        {
            var email = await GetUserEmailAsync();
            var message = await _context.Messages.FindAsync(id);
            if (message == null) return NotFound();

            bool ownTrash = message.ReceiverEmail == email && message.IsDeleted && !message.IsDraft;
            bool ownDraft = message.SenderEmail == email && message.IsDraft;
            if (!ownTrash && !ownDraft) return Forbid();

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Mesaj kalıcı olarak silindi.";
            return ownDraft ? RedirectToAction("Drafts") : RedirectToAction("Trash");
        }

        // ---------------------------------------------------------------- Yardımcı metotlar
        private async Task<Message?> GetOwnReceivedMessageAsync(int id)
        {
            var email = await GetUserEmailAsync();
            return await _context.Messages.FirstOrDefaultAsync(x => x.MessageId == id && x.ReceiverEmail == email && !x.IsDraft);
        }

        private async Task UpdateOwnMessagesAsync(int[] ids, Action<Message> update)
        {
            var email = await GetUserEmailAsync();
            var messages = await _context.Messages
                .Where(x => ids.Contains(x.MessageId) && x.ReceiverEmail == email && !x.IsDraft)
                .ToListAsync();

            messages.ForEach(update);
            await _context.SaveChangesAsync();
        }

        private bool IsAjax() => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        private IActionResult BackOrOk()
        {
            if (IsAjax()) return Ok();

            var referer = Request.Headers.Referer.ToString();
            if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(new Uri(referer).PathAndQuery))
                return LocalRedirect(new Uri(referer).PathAndQuery);

            return RedirectToAction("Inbox");
        }

        private static bool IsValidEmail(string email) =>
            Regex.IsMatch(email, @"^[^\s@<>()\[\],;:""]+@[^\s@<>()\[\],;:""]+\.[a-zA-Z]{2,}$");

        private static string PlainText(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return "";
            var text = WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " "));
            return Regex.Replace(text, @"\s+", " ").Trim();
        }
    }
}



using ClassmateApii.Data;

namespace ClassmateApii.Models;

// Roman Urdu: Yeh entity DB mein store hoti hai — har course ke liye ek row.
// Isse hum track karte hain ke kaunsa user, kaunse course ke liye registered hai
// aur registration kab expire hogi taake renewal service renew kar sake.
public class ClassroomRegistration
{
    public int Id { get; set; }

    // Roman Urdu: Konse user ki registration hai.
    public int UserId { get; set; }
    public User User { get; set; } = null!;          // EF navigation property

    // Roman Urdu: Google Classroom course ID.
    public string CourseId { get; set; } = string.Empty;

    // Roman Urdu: Human-readable course naam — logs aur debugging ke liye.
    public string CourseName { get; set; } = string.Empty;

    // Roman Urdu: Google ne jo registration ID assign ki hai — delete/renew ke liye zaroori.
    public string GoogleRegistrationId { get; set; } = string.Empty;

    // Roman Urdu: Humara apna channel ID jo hum Google ko registration mein bhejte hain.
    public string ChannelId { get; set; } = string.Empty;

    // Roman Urdu: Secret token jo webhook request verify karne ke liye use hota hai.
    // Yeh wahi value hai jo Google X-Goog-Channel-Token header mein wapas bhejta hai.
    public string ChannelToken { get; set; } = string.Empty;

    // Roman Urdu: Kab expire hogi — 7 din baad. Renewal service isse check karta hai.
    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
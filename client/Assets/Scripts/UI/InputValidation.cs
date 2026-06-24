// ============================================
// InputValidation — client-side form validation helpers
// ============================================
using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveRoyale.UI
{
    public static class InputValidation
    {
        // ---- Validators ----

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email.Trim(),
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase);
        }

        public static bool IsValidPassword(string pwd) =>
            !string.IsNullOrEmpty(pwd) && pwd.Length >= 8;

        public static bool IsValidUsername(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string t = name.Trim();
            return t.Length >= 3 && t.Length <= 20 &&
                   Regex.IsMatch(t, @"^[a-zA-Z0-9_\-]+$");
        }

        public static bool IsValidRoomName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return name.Trim().Length >= 2 &&
                   name.Trim().Length <= Core.GameConstants.MAX_ROOM_NAME_LENGTH;
        }

        // ---- Field visual feedback ----

        public static bool ValidateField(TMP_InputField field, Func<string, bool> validator,
            TMP_Text errorLabel, string errorMsg)
        {
            bool ok = validator(field?.text ?? "");
            if (errorLabel != null)
                errorLabel.text = ok ? "" : errorMsg;
            if (field != null)
            {
                var img = field.GetComponent<Image>();
                if (img) img.color = ok ? Color.white : new Color(1f, 0.8f, 0.8f);
            }
            return ok;
        }

        /// <summary>Shows a red border on an invalid field and clears it when valid.</summary>
        public static void AttachRealtimeValidation(TMP_InputField field,
            Func<string, bool> validator, TMP_Text errorLabel, string errorMsg)
        {
            if (field == null) return;
            field.onValueChanged.AddListener(val =>
                ValidateField(field, validator, errorLabel, errorMsg));
        }
    }

    // ---- Attach to TMP_InputField GameObjects ----
    [RequireComponent(typeof(TMP_InputField))]
    public class InputValidator : MonoBehaviour
    {
        public enum ValidationType { Email, Password, Username, RoomName, NotEmpty, Custom }

        [SerializeField] private ValidationType _type;
        [SerializeField] private TMP_Text       _errorLabel;
        [SerializeField] private string         _customErrorMsg = "Invalid input";
        [SerializeField] private int            _minLength = 1;
        [SerializeField] private int            _maxLength = 100;

        private TMP_InputField _field;

        void Awake()
        {
            _field = GetComponent<TMP_InputField>();
            _field.onValueChanged.AddListener(OnChanged);
        }

        private void OnChanged(string val)
        {
            bool ok = _type switch
            {
                ValidationType.Email     => InputValidation.IsValidEmail(val),
                ValidationType.Password  => InputValidation.IsValidPassword(val),
                ValidationType.Username  => InputValidation.IsValidUsername(val),
                ValidationType.RoomName  => InputValidation.IsValidRoomName(val),
                ValidationType.NotEmpty  => !string.IsNullOrWhiteSpace(val),
                ValidationType.Custom    => val.Length >= _minLength && val.Length <= _maxLength,
                _                        => true
            };

            if (_errorLabel)
            {
                _errorLabel.text = ok ? "" : _customErrorMsg;
                _errorLabel.gameObject.SetActive(!ok && !string.IsNullOrEmpty(val));
            }

            var bg = _field.GetComponent<Image>();
            if (bg) bg.color = ok || string.IsNullOrEmpty(val)
                ? Color.white
                : new Color(1f, 0.85f, 0.85f);
        }

        public bool IsValid() => _type switch
        {
            ValidationType.Email    => InputValidation.IsValidEmail(_field.text),
            ValidationType.Password => InputValidation.IsValidPassword(_field.text),
            ValidationType.Username => InputValidation.IsValidUsername(_field.text),
            ValidationType.RoomName => InputValidation.IsValidRoomName(_field.text),
            ValidationType.NotEmpty => !string.IsNullOrWhiteSpace(_field.text),
            ValidationType.Custom   => _field.text.Length >= _minLength && _field.text.Length <= _maxLength,
            _                       => true
        };
    }
}

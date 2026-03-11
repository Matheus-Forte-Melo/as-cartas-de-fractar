using TMPro;
using UnityEngine;

public class CardView : MonoBehaviour
{
    [SerializeField] private TMP_Text _equationText;
    [SerializeField] private GameObject _cardFront;
    [SerializeField] private GameObject _cardBack;

    private bool _isFaceDown;

    public bool IsFaceDown
    {
        get => _isFaceDown;
        set
        {
            _isFaceDown = value;
            _cardFront.SetActive(!value);
            _cardBack.SetActive(value);
        }
    }

    private void Awake()
    {
        // Auto-wire dos componentes
        if (_cardFront == null)
            _cardFront = transform.Find("CardFront")?.gameObject;
        if (_cardBack == null)
            _cardBack = transform.Find("CardBack")?.gameObject;
        if (_equationText == null && _cardFront != null)
            _equationText = _cardFront.GetComponentInChildren<TMP_Text>();
    }

    public void Setup(string equation, bool faceDown = false)
    {
        _equationText.text = equation;
        IsFaceDown = faceDown;
    }
}

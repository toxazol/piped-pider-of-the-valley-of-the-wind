using System;
using UnityEngine;
using UnityEngine.UI;

public class FixedLooper : MonoBehaviour
{
    public struct Note
    {
        public bool isActive;
        public Toggle toggle;
    }
    public Note[] notes;
    public event Action OnNoteChange;

    [SerializeField] private int noteDivision = 8;
    [SerializeField] private int trackLen;

    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private int tickEvery = 0;
    [SerializeField] private bool isPause = false;
    [SerializeField] private bool isHighlighted = false;
    [SerializeField] private Color highColor;
    [SerializeField] private bool isRhythmGame = false;
    [SerializeField] private GameObject hitIndicator;
    [SerializeField] private GameObject targetRow;
    [SerializeField] private AttackZone playerAttack;
    [SerializeField] private GameObject targetHitRow;
    [SerializeField] private LooperSettings settings;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color initHitRowColor;
    private int frame = 0;
    private int pulse = 0;
    private AudioSource audioSource;
    

    void Start()
    {
        noteDivision = settings.NoteDivision;
        isHighlighted = settings.IsHighlighted;
        hitIndicator = settings.HitIndicator;
        playerAttack = settings.PlayerAttack;

        if(isRhythmGame)
        {
            selectedColor = Color.red;
            initHitRowColor = Color.white;
        }

        trackLen = settings.Bars * noteDivision; 
        notes = new Note[trackLen];
        
        audioSource = GetComponent<AudioSource>();
        if(tickEvery > 0)
        {
            InitTicker(tickEvery);
        }
        OnNoteChange?.Invoke(); // to trigger isGuessed init
        InitGrid();
    }

    void InitTicker(int tickEvery)
    {
        for(int i = 0; i < trackLen; i++)
        {
            if(i%tickEvery != 0 ) continue;
            notes[i].isActive = true;
        }
    }

    void InitGrid()
    {
        int barLen = noteDivision;
        for(int i = 0; i < trackLen; i++)
        {
            int index = i;
            var cell = Instantiate(cellPrefab, targetRow.transform);
            ColorizeCell(cell, i, barLen);
            var toggle = cell.GetComponent<Toggle>();
            notes[i].toggle = toggle;
            toggle.isOn = notes[i].isActive;
            toggle.onValueChanged.AddListener((val)=>{
                notes[index].isActive = val;
                OnNoteChange?.Invoke();
                // Utilities.logArr(notes.Select(n=>n.isActive).ToArray());
            });
        }
    }

    void ColorizeCell(GameObject cell, int i, int barLen)
    {
        if(i % barLen == 0)
            isHighlighted = !isHighlighted;
        if(!isHighlighted)
            return;
        cell.GetComponent<Image>().color = highColor;
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        if(isPause)
            return;
        if(pulse >= trackLen)
            pulse = 0;

        MoveCaret();

        if(frame % settings.Fpb == 0)
        {
            if(isRhythmGame)
                UpdateHitRow();
            if(notes[pulse].isActive)
            {
                audioSource.Play();
            }
            
                
            pulse++;
        }
            
        frame++;
    }

    void UpdateHitRow()
    {
        int i = pulse;
        foreach (Transform child in targetHitRow.transform)
        {
            var targetChild = child.gameObject.GetComponent<Image>();
            if(!notes[i++].isActive)
                targetChild.color = initHitRowColor;
            else
                targetChild.color = selectedColor;

            if(i >= trackLen)
                i = 0;
        }
    }


    void MoveCaret()
    {
        var curBtnPlayed = notes[pulse].toggle.transform.Find("Played").gameObject;
        curBtnPlayed.SetActive(true);
        var prevInd = pulse - 1 >= 0 ? pulse - 1 : trackLen - 1;
        var prevBtnPlayed = notes[prevInd].toggle.transform.Find("Played").gameObject;
        prevBtnPlayed.SetActive(false);
    }

    void OnToggleMusic()
    {
        isPause = !isPause;
    }

    void OnFire()
    {
        if(!isRhythmGame || isPause)
            return;
        
        int fireFrame = frame + settings.UserFrameDelta;
        int from = fireFrame - settings.UserFrameTolerance;
        int to = fireFrame + settings.UserFrameTolerance;
        for(int i = fireFrame, step = 1; i >= from && i <= to;)
        {
            int firePulse = GetPulseFromFrame(i);   
            if(firePulse >= 0 && notes[firePulse].isActive)
            {
                BuffDamamge();
                ShowHit();
                return;
            }
            // go one step at a time further from the center
            i += step;
            step *= -1;
            step += step > 0 ? 1 : -1;
        }
    }

    void BuffDamamge()
    {
        playerAttack.AttackDamage = settings.DamageRhythm;
        playerAttack.KnockbackPower = settings.KnockBackRhythm;
        Invoke(nameof(StopBuffDamamge), settings.BuffSecs);
    }
    void StopBuffDamamge()
    {
        playerAttack.AttackDamage = settings.DamageDefault;
        playerAttack.KnockbackPower = settings.KnockBackDefault;
    }

    void ShowHit()
    {
        hitIndicator.GetComponent<Image>().color = Color.green;
        Invoke(nameof(StopIndication), settings.IndicationSecs);
    }

    void StopIndication()
    {
        hitIndicator.GetComponent<Image>().color = Color.white;
    }

    int GetPulseFromFrame(int frame)
    {
        if(frame % settings.Fpb > 0)
            return -1;

        int pulses = frame / settings.Fpb + 1;
        int curPulse = pulses % trackLen;

        return curPulse; 
    }

}

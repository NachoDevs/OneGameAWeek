﻿using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using SimpleJSON;

enum CombatFace
{
    PlayerSelect,
    PlayerAttack,
    AIFace,
    ResetFace
}

public class CombatSceneManager : MonoBehaviour
{

    [Header("UI")]
    public Button endTurnButton;
    public TextMeshProUGUI stateText;
    public Transform characterInfoPanelParent;
    public Transform enemyInfoPanelParent;
    public GameObject characterInfoPanel;
    public GameObject enemyInfoPanel;
    public GameObject abilityButton;
    public GameObject abilitiesPanel;
    public GameObject characterSelectionArrow;

    [Header("Character")]
    public Transform charactersParent;
    public GameObject princeCharacter;
    public GameObject elfCharacter;
    public GameObject pirateCaptainCharacter;

    public GameObject background;

    public List<Sprite> backgrounds;

    float[,] m_partyPos;

    CombatFace m_currFace;

    Camera m_cam;

    Character m_selectedCharacter;
    Character m_selectedEnemy;

    GameObject playerArrow;
    GameObject enemyArrow;

    RaycastHit2D m_hit;

    List<GameObject> m_characters;
    List<GameObject> m_party;
    List<GameObject> m_enemies;
    List<GameObject> m_infoPanels;

    void Awake()
    {
        m_characters = new List<GameObject>();
        m_party = new List<GameObject>();
        m_enemies = new List<GameObject>();
        m_infoPanels = new List<GameObject>();
        m_cam = Camera.main;
    }

    // Start is called before the first frame update
    void Start()
    {
        LoadCharacters();
        LoadAbilities();
        HideAbilitiesPanel();
        m_currFace = CombatFace.PlayerSelect;
    }

    // Update is called once per frame
    void Update()
    {
        HandleCombatState();
    }

    void StartTurn()
    {
        endTurnButton.interactable = true;
        m_currFace = CombatFace.PlayerSelect;
    }

    public void EndTurn()
    {
        endTurnButton.interactable = false;
        m_currFace = CombatFace.AIFace;
    }

    public void LoadMainMap()
    {
        SaveSystem.SaveGame(SaveSystem.GenerateGameData(m_characters, null, m_partyPos));

        SceneManager.LoadScene(0);
    }

    void HandleCombatState()
    {
        switch (m_currFace)
        {
            default:    // Playerface is the default state
            case CombatFace.PlayerSelect:
                if (Input.GetMouseButtonUp(0))
                {
                    m_hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

                    HideAbilitiesPanel();
                    try
                    {
                        if (m_hit.collider.gameObject.GetComponentInParent<Player>() != null)
                        {
                            m_selectedCharacter = m_hit.collider.gameObject.GetComponentInParent<Character>();
                            if(m_selectedCharacter.hasAttacked)
                            {
                                StartCoroutine(GameManager.PrintText(stateText, "This unit has already attacked!"));
                                m_selectedCharacter = null;
                            }
                            else
                            {
                                m_currFace = CombatFace.PlayerAttack;
                                ShowAbilitiesPanel();
                                SelectCharacter(m_selectedCharacter);
                            }
                        }
                    }
                    catch (Exception e) { GameManager.PrintException(e); }
                }
                break;
            case CombatFace.PlayerAttack:
                if(Input.GetMouseButtonUp(0))
                {
                    m_hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

                    try
                    {
                        if (m_hit.collider.gameObject.GetComponentInParent<Enemy>() != null)
                        {
                            m_selectedEnemy = m_hit.collider.gameObject.GetComponentInParent<Character>();
                            SelectCharacter(m_selectedEnemy);
                        }
                        else if (m_hit.collider.gameObject.GetComponentInParent<Character>() == m_selectedCharacter)
                        {
                            UnselectCharacter(m_selectedCharacter);
                            UnselectCharacter(m_selectedEnemy);
                            HideAbilitiesPanel();
                            m_currFace = CombatFace.PlayerSelect;
                        }
                    }
                    catch (Exception e) { GameManager.PrintException(e); }
                }
                break;
            case CombatFace.AIFace:
                HandleAI();
                break;
            case CombatFace.ResetFace:
                CheckIfCombatHasEneded();
                ResetCombatTurn();
                break;
        }
    }

    void HandleAI()
    {
        foreach(GameObject enemy in m_enemies)
        {
            Character enemyC = enemy.GetComponent<Character>();

            if(!enemyC.isDead)
            {
                Character target = m_party[UnityEngine.Random.Range(0, m_party.Count)].GetComponent<Character>();
                Ability ability = enemyC.abilities[UnityEngine.Random.Range(0, enemyC.abilities.Count)];

                enemyC.UseAbility(ability, target);

                //CheckIfCombatHasEneded();
            }
        }
        m_currFace = CombatFace.ResetFace;
    }

    void ResetCombatTurn()
    {
        foreach(GameObject character in m_party)
        {
            character.GetComponent<Character>().NewCombatTurn();
        }

        m_currFace = CombatFace.PlayerSelect;
    }

    void EndCombat()
    {
        HideAbilitiesPanel();

        foreach (GameObject character in m_party)
        {
            character.GetComponent<Character>().EndCombat();
        }

        m_currFace = CombatFace.PlayerSelect;

        LoadMainMap();
    }

    void CheckIfCombatHasEneded()
    {
        bool combatEnded = true;
        foreach (GameObject character in m_enemies)
        {
            if (!character.GetComponent<Character>().isDead)
            {
                combatEnded = false;
                break;
            }
        }

        if(combatEnded)
        {
            EndCombat();
        }

        foreach (GameObject character in m_party)
        {
            if (character.GetComponent<Character>().isDead)
            {
                combatEnded = true;
            }
            else
            {
                combatEnded = false;
                break;
            }
        }

        if (combatEnded)
        {
            EndCombat();
        }
    }

    void LoadCharacters()
    {
        GameData gd = SaveSystem.LoadGame();
        
        if (gd != null)
        {
            int numOfCharacters = gd.charsHealth.Length;
            m_partyPos = new float[2, numOfCharacters];

            int allyCount = 0, enemyCount = 0;
            for(int i = 0; i < numOfCharacters; ++i)
            {
                GameObject character = Character.id_prefab[i];
                Character characterC = character.GetComponent<Character>();

                m_characters.Add(character);

                bool inCombat = false;
                foreach(int index in gd.charactersInvolvedInCombat)
                {
                    if(characterC.characterIndex == index)
                    {
                        inCombat = true;
                        break;
                    }
                }

                m_partyPos[0, i] = gd.charsPositions[0, i];
                m_partyPos[1, i] = gd.charsPositions[1, i];

                if (!inCombat)
                {
                    continue;
                }

                if(!characterC.isDead)
                {
                    // To make sure the index is set before the rest of the operations
                    characterC.CheckIndex();
                    // Position
                    character.transform.position = new Vector3((character.GetComponent<Enemy>() == null) ? (-4f - ((allyCount++) * 2)): (4f + ((enemyCount++) * 2) ), 0f, -1f);
                    // Local scale
                    character.transform.localScale = new Vector3(3, 3, 1);
                    // Info Panel
                    SetUpCharacterInfoPanel(character.GetComponent<Character>(), (character.GetComponent<Player>() == null) ? true : false);
                    // Disable movement script
                    character.GetComponent<CharacterMovement>().enabled = false;
                    // Flip sprite if enemy
                    character.GetComponentInChildren<SpriteRenderer>().flipX = (character.GetComponent<Enemy>() != null) ? true : false;
                    // Load health
                    characterC.health = gd.charsHealth[i];
                    // Load attackBoost
                    characterC.damageBoost= gd.charsDamageBoost[characterC.characterIndex];
                    // To check if the can move after the combat
                    characterC.hasMoved = gd.charsHaveMoved[characterC.characterIndex];
                    // For combat movement
                    characterC.combatInitialPosition = character.transform.position;
                    // Getting ready for the combat
                    characterC.NewCombatTurn();

                    if (character.GetComponent<Player>() != null)
                    {
                        m_party.Add(character);
                        SelectBackground(i);
                    }
                    else
                    {
                        m_enemies.Add(character);
                    }

                }
            }
        }
    }

    void SetUpCharacterInfoPanel(Character t_character, bool t_isEnemy)
    {
        GameObject panel = Instantiate((t_isEnemy)? enemyInfoPanel : characterInfoPanel
                                        , (t_isEnemy) ? enemyInfoPanelParent : characterInfoPanelParent);

        panel.GetComponentInChildren<TextMeshProUGUI>().text = t_character.characterName;
        t_character.healthBar = panel.GetComponentInChildren<Slider>();

        m_infoPanels.Add(panel);
    }

    void ShowAbilitiesPanel()
    {
        abilitiesPanel.SetActive(true);

        foreach (Ability ability in m_selectedCharacter.abilities)
        {
            GameObject button = Instantiate(abilityButton, abilitiesPanel.transform);

            button.GetComponentInChildren<TextMeshProUGUI>().text = ability.type.ToString();

            button.GetComponent<Button>().onClick.AddListener(() => 
            {
                if (ability.damage != 0)
                {
                    if (m_selectedEnemy == null)
                    {
                        StartCoroutine(GameManager.PrintText(stateText, "You need to select a taget!"));
                        return;
                    }
                }

                m_selectedCharacter.UseAbility(ability, m_selectedEnemy);
                m_currFace = CombatFace.PlayerSelect;
                UnselectCharacter(m_selectedCharacter);
                UnselectCharacter(m_selectedEnemy);

                CheckIfCombatHasEneded();

                bool partyFinished = true;
                foreach (GameObject character in m_party)
                {
                    if (!character.GetComponent<Character>().hasAttacked)
                    {
                        partyFinished = false;
                        break;
                    }
                }
                if (partyFinished)
                {
                    m_currFace = CombatFace.AIFace;
                }

                HideAbilitiesPanel();
            });
        }
    }

    void HideAbilitiesPanel()
    {
        abilitiesPanel.SetActive(false);

        foreach (Transform abilityButton in abilitiesPanel.transform)
        {
            Destroy(abilityButton.gameObject);
        }
    }

    void LoadAbilities()
    {
        string path = Application.dataPath + "/JSONs/characterAbilities.json";
        string jsonString = File.ReadAllText(path);

        JSONObject abilitiesJSON = JSON.Parse(jsonString) as JSONObject;

        List<GameObject> characters = new List<GameObject>();
        characters.AddRange(m_party);
        characters.AddRange(m_enemies);
        foreach(GameObject character in characters)
        {
            Character charC = character.GetComponent<Character>();

            foreach (var characterJSON in abilitiesJSON)
            {
                if (characterJSON.Key.ToLower() != charC.characterName.ToLower())
                {
                    continue;
                }

                foreach (var ability in characterJSON.Value)
                {
                    Ability newAbility = new Ability();
                    newAbility.SetAbilityAs(ability.Value);
                    charC.abilities.Add(newAbility);
                }
            }

        }
    }

    void SelectCharacter(Character t_character)
    {
        GameObject arrow = Instantiate(characterSelectionArrow, t_character.gameObject.transform);
        arrow.transform.position = new Vector3(t_character.gameObject.transform.position.x
                                            , t_character.gameObject.transform.position.y + 1
                                            , t_character.gameObject.transform.position.z);
        arrow.transform.localScale /= 3;

        try
        {
            if(t_character.GetComponent<Player>() != null)
            {
                Destroy(playerArrow);
                arrow.GetComponentInChildren<SpriteRenderer>().color = Color.green;
                playerArrow = arrow;
            }
            else
            {
                Destroy(enemyArrow);
                arrow.GetComponentInChildren<SpriteRenderer>().color = Color.red;
                enemyArrow = arrow;
            }
        } catch(Exception e) { GameManager.PrintException(e); }
    }

    void UnselectCharacter(Character t_character)
    {
        try
        {
            if(t_character == null)
            {
                return;
            }

            if (t_character.GetComponent<Player>() != null)
            {
                m_selectedCharacter = null;
                Destroy(playerArrow);
            }
            else
            {
                m_selectedEnemy = null;
                Destroy(enemyArrow);
            }
        } catch(Exception e) { GameManager.PrintException(e); }
    }

    void SelectBackground(int t_charIndex)
    {
        Sprite backgroundSprite = backgrounds[0];

        List<Vector2> foresTile = new List<Vector2>();
        List<Vector2> castleTile = new List<Vector2>();
        List<Vector2> villageTile = new List<Vector2>();

        {
            foresTile.Add(new Vector2(4.5f, 63.5f));
            foresTile.Add(new Vector2(4.5f, 62.5f));
            foresTile.Add(new Vector2(4.5f, 61.5f));
            foresTile.Add(new Vector2(5.5f, 62.5f));
            foresTile.Add(new Vector2(5.5f, 61.5f));
            foresTile.Add(new Vector2(6.5f, 62.5f));
            foresTile.Add(new Vector2(6.5f, 61.5f));
            foresTile.Add(new Vector2(6.5f, 60.5f));
            foresTile.Add(new Vector2(7.5f, 61.5f));
            foresTile.Add(new Vector2(7.5f, 62.5f));
            foresTile.Add(new Vector2(11.5f, 61.5f));
            foresTile.Add(new Vector2(11.5f, 60.5f));
            foresTile.Add(new Vector2(12.5f, 61.5f));
            foresTile.Add(new Vector2(12.5f, 60.5f));
            foresTile.Add(new Vector2(13.5f, 60.5f));
            foresTile.Add(new Vector2(17.5f, 64.5f));
            foresTile.Add(new Vector2(18.5f, 65.5f));
            foresTile.Add(new Vector2(18.5f, 64.5f));
            foresTile.Add(new Vector2(18.5f, 63.5f));
            foresTile.Add(new Vector2(19.5f, 64.5f));
            foresTile.Add(new Vector2(19.5f, 63.5f));
            foresTile.Add(new Vector2(20.5f, 57.5f));
            foresTile.Add(new Vector2(20.5f, 56.5f));
            foresTile.Add(new Vector2(20.5f, 55.5f));
            foresTile.Add(new Vector2(21.5f, 55.5f));
            foresTile.Add(new Vector2(21.5f, 54.5f));
            foresTile.Add(new Vector2(22.5f, 54.5f));
            foresTile.Add(new Vector2(12.5f, 53.5f));
            foresTile.Add(new Vector2(12.5f, 52.5f));
            foresTile.Add(new Vector2(13.5f, 53.5f));
            foresTile.Add(new Vector2(13.5f, 52.5f));
            foresTile.Add(new Vector2(14.5f, 52.5f));
        }

        {
            villageTile.Add(new Vector2(13.5f, 49.5f));
            villageTile.Add(new Vector2(13.5f, 48.5f));
            villageTile.Add(new Vector2(14.5f, 49.5f));
            villageTile.Add(new Vector2(20.5f, 60.5f));
            villageTile.Add(new Vector2(20.5f, 59.5f));
            villageTile.Add(new Vector2(4.5f, 65.5f));
            villageTile.Add(new Vector2(4.5f, 64.5f));
            villageTile.Add(new Vector2(5.5f, 65.5f));
            villageTile.Add(new Vector2(5.5f, 64.5f));
        }

        {
            castleTile.Add(new Vector2(12.5f, 64.5f));
            castleTile.Add(new Vector2(12.5f, 65.5f));
            castleTile.Add(new Vector2(12.5f, 66.5f));
            castleTile.Add(new Vector2(12.5f, 67.5f));
            castleTile.Add(new Vector2(13.5f, 64.5f));
            castleTile.Add(new Vector2(13.5f, 65.5f));
            castleTile.Add(new Vector2(13.5f, 66.5f));
            castleTile.Add(new Vector2(13.5f, 67.5f));
            castleTile.Add(new Vector2(15.5f, 64.5f));
            castleTile.Add(new Vector2(15.5f, 65.5f));
            castleTile.Add(new Vector2(15.5f, 66.5f));
            castleTile.Add(new Vector2(15.5f, 67.5f));
            castleTile.Add(new Vector2(16.5f, 64.5f));
            castleTile.Add(new Vector2(16.5f, 65.5f));
            castleTile.Add(new Vector2(16.5f, 66.5f));
            castleTile.Add(new Vector2(16.5f, 67.5f));
        }

        Vector2 charPos = new Vector2(m_partyPos[0, t_charIndex], m_partyPos[1, t_charIndex]);

        if(foresTile.Contains(charPos))
        {
            backgroundSprite = backgrounds[1];
        }

        if (villageTile.Contains(charPos))
        {
            backgroundSprite = backgrounds[2];
        }

        if (castleTile.Contains(charPos))
        {
            backgroundSprite = backgrounds[3];
        }

        background.GetComponent<SpriteRenderer>().sprite = backgroundSprite;
    }
}

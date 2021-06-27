using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using rand = UnityEngine.Random;

public class MinecraftSurvival : MonoBehaviour
{

	// Bomb Vars
	public KMBombInfo bomb;
	public KMAudio bAudio;

	public Material[] _dimensionMaterials;
	public Material[] _swordMaterials;
	public Material[] _mobMaterials;

	public KMSelectable[] _actionButtons; // Inventory and dimensions and fighting
	public KMSelectable[] _resourceButtons; // Resource buttons
	public KMSelectable[] _inventoryButtons; // Inventory/crafting buttons
	public KMSelectable[] _allResourceInventoryIndicies; // Contains all resource indicies for the array
	public KMSelectable _module;

	public SpriteRenderer[] _fullHunger;
	public SpriteRenderer[] _emptyHunger;
	public SpriteRenderer[] _fullHearts;
	public SpriteRenderer[] _halfHearts;
	public SpriteRenderer[] _emptyHearts;
	public SpriteRenderer[] _regenHearts;

	public MeshRenderer[] _actionButtonMeshes;
	public MeshRenderer[] _resourceButtonMeshes;
	public MeshRenderer[] _inventoryButtonMeshes;
	public MeshRenderer[] _moduleMeshes;
	public MeshRenderer _mobIndicator;

	public TextMesh _materialValueText;
	public TextMesh _mobHealthIndicator;

	public Vector3 _resourceTextPos;
	public Vector3 _invTextPos;

	// Specifically for Logging
	static int _modIDCount = 1;
	int _modID;
	private bool _modSolved;

	// Module Vars
	int _gMonsterIndex = 0;
	int _gMonsterHealth = 0;
	int _gMonsterDamage = 0;
	int _dimensionIndex = 0;
	int _resourceUntilFight = 0; // Number of resource pick ups before
	int _playerHealth = 0;
	int _playerDamage = 0;
	int _playerProc = 0;
	int _playerHunger = 0;

	int[] _monsterHealth = new int[] { 30, 20, 15, 20, 20, 40, 30, 40, 50, 30, 500 };
	int[] _monsterDamage = new int[] { 6, 4, 4, 20, 4, 6, 5, 8, 10, 5, 8 };
	int[] _materialValues = new int[44];
	int[] _swordDamages = new int[] { 6, 9, 12, 15 };
	int[] _armorProc = new int[] { 4, 8 };
	int[] _materialValueIndex = new int[] { 42, 19, 20, 21, 22, 23, 25, 24, 34, 26, 27, 28, 29, 25, 30, 31 };

	string _gMonsterName = "";

	string[] _monsterIndex = new string[] { "Zombie", "Skeleton", "Spider", "Creeper", "Slime", "Pigman", "Blaze", "Wither Skeleton", "Enderman", "Shulker", "Ender Dragon" };
	string[][] _monsterTable = new string[][] {
		new string[] { "Zombie", "Skeleton", "Spider", "Creeper", "Enderman", "Slime", "Enderman" },
		new string[] { "Skeleton", "Blaze", "Pigman", "Enderman", "Blaze", "Enderman", "Wither Skeleton", "Enderman", "Blaze" },
		new string[] { "Enderman", "Shulker" }
	};

	bool _isNetherUnlocked = false;
	bool _isEndUnlocked = false;
	bool _fightStarted = false;
	bool _dragonFightStarted = false;
	bool _isAnimating = false;
	bool _inInventory = false;
	bool _fightReset = false;
	bool _dragonDefeated = false;
    bool _startedModule = false;

	void Awake()
	{
		_modID = _modIDCount++;

		for (int i = 0; i < _materialValues.Length; i++)
		{
			_materialValues[i] = 0;
		}

		foreach (KMSelectable action in _actionButtons)
		{
			action.OnInteract += delegate () { if (_modSolved) { return false; } action.AddInteractionPunch(); ActionButton(action); return false; };
			action.OnHighlight += delegate () { if (_isAnimating || _modSolved) { return; } if (Array.IndexOf(_actionButtons, action) == 6) { UpdateText(4); } return; };
			action.OnHighlightEnded += delegate () { _materialValueText.text = ""; return; };
		}

		foreach (KMSelectable resource in _resourceButtons)
		{
			resource.OnInteract += delegate () { if (_modSolved) { return false; } resource.AddInteractionPunch(); ResourceButton(resource); return false; };
			resource.OnHighlight += delegate () { if (_isAnimating || _modSolved) { return; } UpdateAmountDisplayAct(resource); return; };
			resource.OnHighlightEnded += delegate () { _materialValueText.text = ""; return; };
		}

		foreach (KMSelectable inv in _allResourceInventoryIndicies)
		{
			inv.OnInteract += delegate () { if (_modSolved) { return false; } inv.AddInteractionPunch(); InventoryButton(inv); return false; };
			inv.OnHighlight += delegate () { if (_isAnimating || _modSolved) { return; } UpdateAmountDisplayInv(inv); return; };
			inv.OnHighlightEnded += delegate () { _materialValueText.text = ""; return; };
		}
		_resourceUntilFight = rand.Range(6, 13);
	}

	void Start()
	{
		StartCoroutine(UpdateHighlights());
		StartCoroutine(UpHunger());
	}

	void ActionButton(KMSelectable but)
	{
		int index = Array.IndexOf(_actionButtons, but);
		switch (index)
		{
			case 0:
				if (_dimensionIndex == 0)
				{
					break;
				}
				_dimensionIndex = 0;
				UpdateModule();
				break;
			case 1:
				if (_dimensionIndex == 1)
				{
					break;
				}
				if (!_isNetherUnlocked && _materialValues[25] >= 14 && _materialValues[43] >= 1)
				{
					_materialValues[25] -= 12;
					_materialValues[43]--;
					_isNetherUnlocked = true;
					_dimensionIndex = 1;
					UpdateModule();
					Debug.LogFormat("[Minecraft Survival #{0}] Unlocked 'The Nether'.", _modID);
				}
				if (!_isNetherUnlocked)
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! Tried to access The Nether without it being unlocked.", _modID);
					break;
				}
				_dimensionIndex = 1;
				UpdateModule();
				break;
			case 2:
				if (_dimensionIndex == 2)
				{
					break;
				}
				if (!_isEndUnlocked && _materialValues[18] >= 12)
				{
					_materialValues[18] -= 12;
					_isEndUnlocked = true;
					_dimensionIndex = 2;
					UpdateModule();
					Debug.LogFormat("[Minecraft Survival #{0}] Unlocked 'The End'.", _modID);
				}
				if (!_isEndUnlocked)
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! Tried to access The End without it being unlocked.", _modID);
					break;
				}
				_dimensionIndex = 2;
				UpdateModule();
				break;
			case 3:
				if (_fightStarted && !_isAnimating)
				{
					FightEventHandler();
				}
				break;
			case 4:
				if (_fightStarted && !_isAnimating)
				{
					EatEventHandler();
				}
				break;
			case 5:
				if (_isAnimating)
				{
					return;
				}
				if (_inInventory)
				{
					StartCoroutine(CloseInventory());
					_inInventory = false;
					return;
				}
				_inInventory = true;
				StartCoroutine(OpenInventory());
				break;
			case 6:
				if (_isAnimating) { return; }
				EatEventHandler();
				UpdateText(4);
				break;
			case 7:
				if (!_dragonDefeated && !_dragonFightStarted)
				{
					StartTheDamnDragonFightAlready();
				}
				break;
			default:
				Debug.LogFormat("[Minecraft Survival #{0}] Unable to change the dimension of the module.", _modID);
				break;
		}
	}

	void ResourceButton(KMSelectable but)
	{
		_playerHunger--;
		UpdateHunger();
		if (_playerHunger == 0)
		{
			GetComponent<KMBombModule>().HandleStrike();
			Debug.LogFormat("[Minecraft Survival #{0}] Strike! You didnt have enough hunger to obtain resources.", _modID);
			_playerHunger = 10;
			UpdateHunger();
			return;
		}
		int current = 0;
		int diff = 0;
		// Overworld: 42 19 20 21 22 23 25 24
		// Nether: 34 26 27 28
		// End: 29 25 30 31
		int index = Array.IndexOf(_resourceButtons, but);
		switch (index)
		{
			// Overworld
			case 0:
				if (!(_materialValues[9] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! You don't have a {1}.", _modID, GetResourceType(9));
					Debug.LogFormat("[Minecraft Survival #{0}] Honestly though, how did you manage that?", _modID);
					break;
				}
                if (!_startedModule)
                    _startedModule = true;
				if (_materialValues[42] >= 64 || _materialValues[42] + 2 >= 64) { _materialValues[42] = 64; UpdateText(42); break; }
				int rnd2 = rand.Range(0, 4);
				if (rnd2 == 0)
					bAudio.PlaySoundAtTransform("wood1", transform);
				else if (rnd2 == 1)
					bAudio.PlaySoundAtTransform("wood2", transform);
				else if (rnd2 == 2)
					bAudio.PlaySoundAtTransform("wood3", transform);
				else if (rnd2 == 3)
					bAudio.PlaySoundAtTransform("wood4", transform);
				_materialValues[42] += 2;
				UpdateText(42);
				current = 42;
				diff = 2;
				break;
			case 1:
				if (!(_materialValues[5] >= 1 || _materialValues[6] >= 1 || _materialValues[7] >= 1 || _materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! Unable to gather {2}. You don't have a {1}.", _modID, GetResourceType(5), GetResourceName(19));
					break;
				}
				int rnd = rand.Range(1, 4);
				if (_materialValues[19] >= 64 || _materialValues[19] + rnd >= 64) { _materialValues[19] = 64; UpdateText(19); break; }
				rnd2 = rand.Range(0, 4);
				if (rnd2 == 0)
					bAudio.PlaySoundAtTransform("stone1", transform);
				else if (rnd2 == 1)
					bAudio.PlaySoundAtTransform("stone2", transform);
				else if (rnd2 == 2)
					bAudio.PlaySoundAtTransform("stone3", transform);
				else if (rnd2 == 3)
					bAudio.PlaySoundAtTransform("stone4", transform);
				_materialValues[19] += rnd;
				UpdateText(19);
				current = 19;
				diff = rnd;
				break;
			case 2:
				if (!(_materialValues[5] >= 1 || _materialValues[6] >= 1 || _materialValues[7] >= 1 || _materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! Unable to gather {2}. You don't have a {1}.", _modID, GetResourceType(5), GetResourceName(20));
					break;
				}
				rnd = rand.Range(3, 6);
				if (_materialValues[20] >= 64 || _materialValues[20] + rnd >= 64) { _materialValues[20] = 64; UpdateText(20); break; }
				rnd2 = rand.Range(0, 4);
				if (rnd2 == 0)
					bAudio.PlaySoundAtTransform("stone1", transform);
				else if (rnd2 == 1)
					bAudio.PlaySoundAtTransform("stone2", transform);
				else if (rnd2 == 2)
					bAudio.PlaySoundAtTransform("stone3", transform);
				else if (rnd2 == 3)
					bAudio.PlaySoundAtTransform("stone4", transform);
				_materialValues[20] += rnd;
				UpdateText(20);
				current = 20;
				diff = rnd;
				break;
			case 3:
				if (_materialValues[5] >= 0 && !(_materialValues[6] >= 1 || _materialValues[7] >= 1 || _materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! Unable to gather {2}. You don't have the right {1}.", _modID, GetResourceType(6), GetResourceName(21));
					break;
				}
				rnd = rand.Range(1, 4);
				if (_materialValues[21] >= 64 || _materialValues[21] + rnd >= 64) { _materialValues[21] = 64; UpdateText(21); break; }
				rnd2 = rand.Range(0, 4);
				if (rnd2 == 0)
					bAudio.PlaySoundAtTransform("stone1", transform);
				else if (rnd2 == 1)
					bAudio.PlaySoundAtTransform("stone2", transform);
				else if (rnd2 == 2)
					bAudio.PlaySoundAtTransform("stone3", transform);
				else if (rnd2 == 3)
					bAudio.PlaySoundAtTransform("stone4", transform);
				_materialValues[21] += rnd;
				UpdateText(21);
				current = 21;
				diff = rnd;
				break;
			case 4:
				if ((_materialValues[5] >= 0 && _materialValues[6] >= 0) && !(_materialValues[7] >= 1 || _materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! Unable to gather {2}. You don't have the right {1}.", _modID, GetResourceType(7), GetResourceName(22));
					break;
				}
				rnd = rand.Range(1, 3);
				if (_materialValues[22] >= 64 || _materialValues[22] + rnd >= 64) { _materialValues[22] = 64; UpdateText(22); break; }
				rnd2 = rand.Range(0, 4);
				if (rnd2 == 0)
					bAudio.PlaySoundAtTransform("stone1", transform);
				else if (rnd2 == 1)
					bAudio.PlaySoundAtTransform("stone2", transform);
				else if (rnd2 == 2)
					bAudio.PlaySoundAtTransform("stone3", transform);
				else if (rnd2 == 3)
					bAudio.PlaySoundAtTransform("stone4", transform);
				_materialValues[22] += rnd;
				UpdateText(22);
				current = 22;
				diff = rnd;
				break;
			case 5:
				if (!(_materialValues[10] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! Unable to gather {2}. You don't have a {1}.", _modID, GetResourceType(10), GetResourceName(23));
					break;
				}
				if (_materialValues[23] >= 64 || _materialValues[23] + 1 >= 64) { _materialValues[23] = 64; UpdateText(23); break; }
				rnd2 = rand.Range(0, 4);
				if (rnd2 == 0)
					bAudio.PlaySoundAtTransform("gravel1", transform);
				else if (rnd2 == 1)
					bAudio.PlaySoundAtTransform("gravel2", transform);
				else if (rnd2 == 2)
					bAudio.PlaySoundAtTransform("gravel3", transform);
				else if (rnd2 == 3)
					bAudio.PlaySoundAtTransform("gravel4", transform);
				_materialValues[23]++;
				UpdateText(23);
				current = 23;
				diff = 1;
				break;
			case 6:
				if ((_materialValues[5] >= 0 && _materialValues[6] >= 0 && _materialValues[7] >= 0) && !(_materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! Unable to gather {2}. You don't have a {1}.", _modID, GetResourceName(8), GetResourceName(25));
					break;
				}
				rnd = rand.Range(1, 3);
				if (_materialValues[25] >= 64 || _materialValues[25] + rnd >= 64) { _materialValues[25] = 64; UpdateText(25); break; }
				rnd2 = rand.Range(0, 4);
				if (rnd2 == 0)
					bAudio.PlaySoundAtTransform("stone1", transform);
				else if (rnd2 == 1)
					bAudio.PlaySoundAtTransform("stone2", transform);
				else if (rnd2 == 2)
					bAudio.PlaySoundAtTransform("stone3", transform);
				else if (rnd2 == 3)
					bAudio.PlaySoundAtTransform("stone4", transform);
				_materialValues[25] += rnd;
				UpdateText(25);
				current = 25;
				diff = rnd;
				break;
			case 7:
				if (!(_materialValues[11] >= 1 || _materialValues[12] >= 1 || _materialValues[13] >= 1 || _materialValues[14] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! Unable to gather {2}. You don't have a {1}.", _modID, GetResourceType(11), GetResourceName(24));
					break;
				}
				rnd = rand.Range(0, 3);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("cow1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("cow2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("cow3", transform);
				rnd = rand.Range(1, 4);
				if (_materialValues[24] >= 64 || _materialValues[24] + rnd >= 64) { _materialValues[24] = 64; UpdateText(24); break; }
				_materialValues[24] += rnd;
				UpdateText(24);
				current = 24;
				diff = rnd;
				break;
			// Nether
			case 8:
				if (!(_materialValues[5] >= 1 || _materialValues[6] >= 1 || _materialValues[7] >= 1 || _materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! You don't have a {1}.", _modID, GetResourceType(5));
					Debug.LogFormat("[Minecraft Survival #{0}] Honestly though, how did you even manage that?", _modID);
					break;
				}
				rnd = rand.Range(2, 5);
				if (_materialValues[34] >= 64 || _materialValues[34] + rnd >= 64) { _materialValues[34] = 64; UpdateText(34); break; }
				rnd2 = rand.Range(0, 4);
				if (rnd2 == 0)
					bAudio.PlaySoundAtTransform("stone1", transform);
				else if (rnd2 == 1)
					bAudio.PlaySoundAtTransform("stone2", transform);
				else if (rnd2 == 2)
					bAudio.PlaySoundAtTransform("stone3", transform);
				else if (rnd2 == 3)
					bAudio.PlaySoundAtTransform("stone4", transform);
				_materialValues[34] += rnd;
				UpdateText(34);
				current = 34;
				diff = rnd;
				break;
			case 9:
				if ((_materialValues[5] >= 0 && _materialValues[6] >= 0) && !(_materialValues[7] >= 1 || _materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! You don't have the right {1}.", _modID, GetResourceType(7));
					Debug.LogFormat("[Minecraft Survival #{0}] How did you even get in the Nether anyway?", _modID);
					break;
				}
				if (_materialValues[26] >= 64 || _materialValues[26] + 2 >= 64) { _materialValues[26] = 64; UpdateText(26); break; }
				rnd2 = rand.Range(0, 4);
				if (rnd2 == 0)
					bAudio.PlaySoundAtTransform("stone1", transform);
				else if (rnd2 == 1)
					bAudio.PlaySoundAtTransform("stone2", transform);
				else if (rnd2 == 2)
					bAudio.PlaySoundAtTransform("stone3", transform);
				else if (rnd2 == 3)
					bAudio.PlaySoundAtTransform("stone4", transform);
				_materialValues[26] += 2;
				UpdateText(26);
				current = 26;
				diff = 2;
				break;
			case 10:
				if ((_materialValues[5] >= 0 && _materialValues[6] >= 0) && !(_materialValues[7] >= 1 || _materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! You don't have the right {1}.", _modID, GetResourceType(7));
					Debug.LogFormat("[Minecraft Survival #{0}] You should go back to the Overworld and unlock the Nether.", _modID);
					break;
				}
				if (_materialValues[27] >= 64 || _materialValues[27] + 2 >= 64) { _materialValues[27] = 64; UpdateText(27); break; }
				rnd2 = rand.Range(0, 3);
				if (rnd2 == 0)
					bAudio.PlaySoundAtTransform("glass1", transform);
				else if (rnd2 == 1)
					bAudio.PlaySoundAtTransform("glass2", transform);
				else if (rnd2 == 2)
					bAudio.PlaySoundAtTransform("glass3", transform);
				_materialValues[27] += 2;
				UpdateText(27);
				current = 27;
				diff = 2;
				break;
			case 11:
				if (!(_materialValues[10] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! You don't have a {1}.", _modID, GetResourceType(10));
					Debug.LogFormat("[Minecraft Survival #{0}] Once you do get one, make sure to get some Flint so you can light the Nether portal.", _modID);
					break;
				}
				if (_materialValues[28] >= 64 || _materialValues[28] + 2 >= 64) { _materialValues[28] = 64; UpdateText(28); break; }
				rnd2 = rand.Range(0, 4);
				if (rnd2 == 0)
					bAudio.PlaySoundAtTransform("sand1", transform);
				else if (rnd2 == 1)
					bAudio.PlaySoundAtTransform("sand2", transform);
				else if (rnd2 == 2)
					bAudio.PlaySoundAtTransform("sand3", transform);
				else if (rnd2 == 3)
					bAudio.PlaySoundAtTransform("sand4", transform);
				_materialValues[28] += 2;
				UpdateText(28);
				current = 28;
				diff = 2;
				break;
			// End
			case 12:
				if (!(_materialValues[5] >= 1 || _materialValues[6] >= 1 || _materialValues[7] >= 1 || _materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! You don't have a {1}.", _modID, GetResourceType(5));
					Debug.LogFormat("[Minecraft Survival #{0}] Did we forget to lock The End again?", _modID);
					break;
				}
				rnd = rand.Range(2, 4);
				if (_materialValues[29] >= 64 || _materialValues[29] + rnd >= 64) { _materialValues[29] = 64; UpdateText(29); break; }
				rnd2 = rand.Range(0, 4);
				if (rnd2 == 0)
					bAudio.PlaySoundAtTransform("stone1", transform);
				else if (rnd2 == 1)
					bAudio.PlaySoundAtTransform("stone2", transform);
				else if (rnd2 == 2)
					bAudio.PlaySoundAtTransform("stone3", transform);
				else if (rnd2 == 3)
					bAudio.PlaySoundAtTransform("stone4", transform);
				_materialValues[29] += rnd;
				UpdateText(29);
				current = 29;
				diff = rnd;
				break;
			case 13:
				if ((_materialValues[5] >= 0 && _materialValues[6] >= 0 && _materialValues[7] >= 0) && !(_materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! You don't have a {1}.", _modID, GetResourceName(8));
					Debug.LogFormat("[Minecraft Survival #{0}] Once you do get one, you should get some Obsidian for a Nether portal.", _modID);
					break;
				}
				rnd = rand.Range(2, 4);
				if (_materialValues[25] >= 64 || _materialValues[25] + rnd >= 64) { _materialValues[25] = 64; UpdateText(25); break; }
				rnd2 = rand.Range(0, 4);
				if (rnd2 == 0)
					bAudio.PlaySoundAtTransform("stone1", transform);
				else if (rnd2 == 1)
					bAudio.PlaySoundAtTransform("stone2", transform);
				else if (rnd2 == 2)
					bAudio.PlaySoundAtTransform("stone3", transform);
				else if (rnd2 == 3)
					bAudio.PlaySoundAtTransform("stone4", transform);
				_materialValues[25] += rnd;
				UpdateText(25);
				current = 25;
				diff = rnd;
				break;
			case 14:
				if (!(_materialValues[5] >= 1 || _materialValues[6] >= 1 || _materialValues[7] >= 1 || _materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! You don't have a {1}.", _modID, GetResourceType(5));
					Debug.LogFormat("[Minecraft Survival #{0}] Good job making it to The End without any resources.", _modID);
					break;
				}
				rnd = rand.Range(3, 5);
				if (_materialValues[30] >= 64 || _materialValues[30] + rnd >= 64) { _materialValues[30] = 64; UpdateText(30); break; }
				rnd2 = rand.Range(0, 4);
				if (rnd2 == 0)
					bAudio.PlaySoundAtTransform("stone1", transform);
				else if (rnd2 == 1)
					bAudio.PlaySoundAtTransform("stone2", transform);
				else if (rnd2 == 2)
					bAudio.PlaySoundAtTransform("stone3", transform);
				else if (rnd2 == 3)
					bAudio.PlaySoundAtTransform("stone4", transform);
				_materialValues[30] += rnd;
				UpdateText(30);
				current = 30;
				diff = rnd;
				break;
			case 15:
				if (!(_materialValues[10] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! You don't have a {1}.", _modID, GetResourceType(10));
					Debug.LogFormat("[Minecraft Survival #{0}] You should get one.");
					break;
				}
				if (_materialValues[31] >= 64 || _materialValues[31] + 2 >= 64) { _materialValues[31] = 64; UpdateText(31); break; }
				rnd2 = rand.Range(0, 4);
				if (rnd2 == 0)
					bAudio.PlaySoundAtTransform("wood1", transform);
				else if (rnd2 == 1)
					bAudio.PlaySoundAtTransform("wood2", transform);
				else if (rnd2 == 2)
					bAudio.PlaySoundAtTransform("wood3", transform);
				else if (rnd2 == 3)
					bAudio.PlaySoundAtTransform("wood4", transform);
				_materialValues[31] += 2;
				UpdateText(31);
				current = 31;
				diff = 2;
				break;
			default:
				Debug.LogFormat("[Minecraft Survival #{0}] Unable to update resources.", _modID);
				break;
		}
		if (current != 0)
		{
			string name = GetResourceName(current);
			if (current == 22 && diff == 1)
				name = "Diamond";
			Debug.LogFormat("[Minecraft Survival #{0}] Gathered {1} {2}.", _modID, diff, name);
		}
		_resourceUntilFight--;
		if (_resourceUntilFight == 0)
		{
			StartFightEventHandler();
		}
	}

	void InsufficientMaterials(int resources, int resourceRequirements, int item, bool plural)
	{
		InsufficientMaterials(new[] { resources }, new[] { resourceRequirements }, item, plural);
	}

	void InsufficientMaterials(int[] resources, int[] resourceRequirements, int item, bool plural)
	{
		GetComponent<KMBombModule>().HandleStrike();
		List<string> resourceNames = new List<string>();
		for (int i = 0; i < resources.Length; i++)
		{
			if (_materialValues[resources[i]] < resourceRequirements[i])
				resourceNames.Add(GetResourceName(resources[i]));
		}
		int count = resourceNames.Count;
		if (count > 2)
			resourceNames[count - 1] = "or " + resourceNames.Last();
		string stuff = count != 2 ? string.Join(", ", resourceNames.ToArray()) : (resourceNames[0] + " or " + resourceNames[1]);
		string result = (plural ? "" : item.EqualsAny(7, 13, 18) ? "an " : "a ") + GetResourceName(item);
		Debug.LogFormat("[Minecraft Survival #{0}] Strike! You don't have the correct item or items [{1}] to craft {2}", _modID, stuff, result);
	}
	
	void InventoryButton(KMSelectable but)
	{
		if (_isAnimating) { return; }
		int index = Array.IndexOf(_allResourceInventoryIndicies, but);
		int current = _materialValues[index];
		if (!index.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 41, 43)) { return; }
		switch (index)
		{
			case 0:
				if (_materialValues[42] >= 1)
				{
					if (_materialValues[0] >= 64 || _materialValues[0] + 4 >= 64) { _materialValues[0] = 64; break; }
					_materialValues[42]--;
					_materialValues[0] += 4;
					UpdateText(0);
					break;
				}
				else
				{
					InsufficientMaterials(42, 1, index, true);
					break;
				}
			case 1:
				if (_materialValues[0] >= 2)
				{
					if (_materialValues[1] >= 64 || _materialValues[1] + 4 >= 64) { _materialValues[1] = 64; break; }
					_materialValues[0] -= 2;
					_materialValues[1] += 4;
					UpdateText(1);
					break;
				}
				else
				{
					InsufficientMaterials(0, 2, index, true);
					break;
				}
			case 2:
				if (_materialValues[19] >= 8)
				{
					if (_materialValues[2] >= 64 || _materialValues[2] + 1 >= 64) { _materialValues[2] = 64; break; }
					_materialValues[19] -= 8;
					_materialValues[2]++;
					UpdateText(2);
					break;
				}
				else
				{
					InsufficientMaterials(19, 8, index, false);
					break;
				}
			case 3:
				if (_materialValues[2] >= 1 && _materialValues[21] >= 1 && _materialValues[20] >= 1)
				{
					int rnd = rand.Range(1, 4);
					if (_materialValues[3] >= 64 || _materialValues[3] + rnd >= 64) { _materialValues[3] = 64; break; }
					_materialValues[21]--;
					_materialValues[20]--;
					_materialValues[3] += rnd;
					UpdateText(3);
					break;
				}
				else
				{
					InsufficientMaterials(new[] { 2, 21, 20 }, new[] { 1, 1, 1 }, index, true);
					break;
				}
			case 4:
				if (_materialValues[2] >= 1 && _materialValues[24] >= 1 && _materialValues[20] >= 1)
				{
					int rnd = rand.Range(1, 4);
					if (_materialValues[4] >= 64 || _materialValues[4] + rnd >= 64) { _materialValues[4] = 64; break; }
					_materialValues[24]--;
					_materialValues[20]--;
					_materialValues[4] += rnd;
					UpdateText(4);
					break;
				}
				else
				{
					InsufficientMaterials(new[] { 2, 24, 20 }, new[] { 1, 1, 1 }, index, true);
					break;
				}
			case 5:
				if (_materialValues[0] >= 3 && _materialValues[1] >= 2)
				{
					if (_materialValues[5] >= 64 || _materialValues[5] + 1 >= 64) { _materialValues[5] = 64; break; }
					_materialValues[0] -= 3;
					_materialValues[1] -= 2;
					_materialValues[5]++;
					UpdateText(5);
					break;
				}
				else
				{
					InsufficientMaterials(new[] { 0, 1 }, new[] { 3, 2 }, index, false);
					break;
				}
			case 6:
				if (_materialValues[5] >= 1 && _materialValues[19] >= 4)
				{
					if (_materialValues[6] >= 64 || _materialValues[6] + 1 >= 64) { _materialValues[6] = 64; break; }
					_materialValues[5]--;
					_materialValues[19] -= 4;
					_materialValues[6]++;
					UpdateText(6);
					break;
				}
				else
				{
					InsufficientMaterials(new[] { 5, 19 }, new[] { 1, 4 }, index, false);
					break;
				}
			case 7:
				if (_materialValues[6] >= 1 && _materialValues[3] >= 5)
				{
					if (_materialValues[7] >= 64 || _materialValues[7] + 1 >= 64) { _materialValues[7] = 64; break; }
					_materialValues[6]--;
					_materialValues[3] -= 5;
					_materialValues[7]++;
					UpdateText(7);
					break;
				}
				else
				{
					InsufficientMaterials(new[] { 6, 3 }, new[] { 1, 5 }, index, false);
					break;
				}
			case 8:
				if (_materialValues[7] >= 1 && _materialValues[22] >= 6)
				{
					if (_materialValues[8] >= 64 || _materialValues[8] + 1 >= 64) { _materialValues[8] = 64; break; }
					_materialValues[7]--;
					_materialValues[22] -= 6;
					_materialValues[8]++;
					UpdateText(8);
					break;
				}
				else
				{
					InsufficientMaterials(new[] { 7, 22 }, new[] { 1, 6 }, index, false);
					break;
				}
			case 9:
				if (_materialValues[0] >= 3 && _materialValues[1] >= 2)
				{
					if (_materialValues[9] >= 64 || _materialValues[9] + 1 >= 64) { _materialValues[9] = 64; break; }
					_materialValues[0] -= 3;
					_materialValues[1] -= 2;
					_materialValues[9]++;
					UpdateText(9);
					break;
				}
				else
				{
					InsufficientMaterials(new[] { 0, 1 }, new[] { 3, 2 }, index, false);
					break;
				}
			case 10:
				if (_materialValues[0] >= 1 && _materialValues[1] >= 2)
				{
					if (_materialValues[10] >= 64 || _materialValues[10] + 1 >= 64) { _materialValues[10] = 64; break; }
					_materialValues[0]--;
					_materialValues[1] -= 2;
					_materialValues[10]++;
					UpdateText(10);
					break;
				}
				else
				{
					InsufficientMaterials(new[] { 0, 1 }, new[] { 1, 2 }, index, false);
					break;
				}
			case 11:
				if (_materialValues[0] >= 2 && _materialValues[1] >= 1)
				{
					if (_materialValues[11] >= 64 || _materialValues[11] + 1 >= 64) { _materialValues[11] = 64; break; }
					_materialValues[0] -= 2;
					_materialValues[1]--;
					_materialValues[11]++;
					UpdateText(11);
					break;
				}
				else
				{
					InsufficientMaterials(new[] { 0, 1 }, new[] { 2, 1 }, index, false);
					break;
				}
			case 12:
				if (_materialValues[11] >= 1 && _materialValues[19] >= 4)
				{
					if (_materialValues[12] >= 64 || _materialValues[12] + 1 >= 64) { _materialValues[12] = 64; break; }
					_materialValues[11]--;
					_materialValues[19] -= 4;
					_materialValues[12]++;
					UpdateText(12);
					break;
				}
				else
				{
					InsufficientMaterials(new[] { 11, 19 }, new[] { 1, 4 }, index, false);
					break;
				}
			case 13:
				if (_materialValues[12] >= 1 && _materialValues[3] >= 5)
				{
					if (_materialValues[13] >= 64 || _materialValues[13] + 1 >= 64) { _materialValues[13] = 64; break; }
					_materialValues[12]--;
					_materialValues[3] -= 5;
					_materialValues[13]++;
					UpdateText(13);
					break;
				}
				else
				{
					InsufficientMaterials(new[] { 12, 3 }, new[] { 1, 5 }, index, false);
					break;
				}
			case 14:
				if (_materialValues[13] >= 1 && _materialValues[22] >= 6)
				{
					if (_materialValues[14] >= 64 || _materialValues[14] + 1 >= 64) { _materialValues[14] = 64; break; }
					_materialValues[13]--;
					_materialValues[22] -= 6;
					_materialValues[14]++;
					UpdateText(14);
					break;
				}
				else
				{
					InsufficientMaterials(new[] { 13, 22 }, new[] { 1, 6 }, index, false);
					break;
				}
			case 15:
				if (_materialValues[3] >= 24)
				{
					if (_materialValues[15] >= 64 || _materialValues[15] + 1 >= 64) { _materialValues[15] = 64; break; }
					_materialValues[3] -= 24;
					_materialValues[15]++;
					UpdateText(15);
					break;
				}
				else
				{
					InsufficientMaterials(3, 24, index, true);
					break;
				}
			case 16:
				if (_materialValues[22] >= 24)
				{
					if (_materialValues[16] >= 64 || _materialValues[16] + 1 >= 64) { _materialValues[16] = 64; break; }
					_materialValues[22] -= 24;
					_materialValues[16]++;
					UpdateText(16);
					break;
				}
				else
				{
					InsufficientMaterials(22, 24, index, true);
					break;
				}
			case 17:
				if (_materialValues[32] >= 1)
				{
					if (_materialValues[17] >= 64 || _materialValues[17] + 2 >= 64) { _materialValues[17] = 64; break; }
					_materialValues[32]--;
					_materialValues[17] += 2;
					UpdateText(17);
					break;
				}
				else
				{
					InsufficientMaterials(32, 1, index, true);
					break;
				}
			case 18:
				if (_materialValues[17] >= 1 && _materialValues[33] >= 1)
				{
					if (_materialValues[18] >= 64 || _materialValues[18] + 1 >= 64) { _materialValues[18] = 64; break; }
					_materialValues[17]--;
					_materialValues[33]--;
					_materialValues[18]++;
					UpdateText(18);
					break;
				}
				else
				{
					InsufficientMaterials(new[] { 17, 33 }, new[] { 1, 1 }, index, false);
					break;
				}
			case 41:
				if (_materialValues[41] >= 1 && _dragonDefeated)
				{
					SolveModule();
					Debug.LogFormat("[Minecraft Survival #{0}] Module solved. Have a good time in your world from now on.", _modID);
					GetComponent<KMBombModule>().HandlePass();
					return;
				}
				else
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}] Strike! You have not obtained that item yet!", _modID);
					break;
				}
			case 43:
				if (_materialValues[3] >= 1 && _materialValues[23] >= 1)
				{
					if (_materialValues[43] >= 64 || _materialValues[43] + 1 >= 64) { _materialValues[43] = 64; break; }
					_materialValues[3]--;
					_materialValues[23]--;
					_materialValues[43]++;
					UpdateText(43);
					break;
				}
				else
				{
					InsufficientMaterials(new[] { 3, 23 }, new[] { 1, 1 }, index, true);
					break;
				}
			default:
				Debug.LogFormat("[Minecraft Survival #{0}] Unable to find the index of the item.", _modID);
				return;
		}
		if (current != _materialValues[index])
		{
			int difference = _materialValues[index] - current;
			string name = GetResourceName(index);
			string count = "";
			if (difference == 1)
			{
				if (index.EqualsAny(7, 13, 18))
					count = " an";
				else if (index == 3)
				{
					count = " an";
					name = "Iron Ingot";
				}
				else if (!index.EqualsAny(15, 16))
					count = " a";
			}
			else
				count = " " + difference;
			Debug.LogFormat("[Minecraft Survival #{0}] Crafted{1} {2}.", _modID, count, name);
		}
		UpdateText(index);
	}

	void UpdateText(int index)
	{
		_materialValueText.text = _materialValues[index].ToString();
	}

	void UpdateAmountDisplayInv(KMSelectable but)
	{
		int index = Array.IndexOf(_allResourceInventoryIndicies, but);
		_materialValueText.text = _materialValues[index].ToString();
	}

	void UpdateAmountDisplayAct(KMSelectable but)
	{
		int index = Array.IndexOf(_resourceButtons, but);
		_materialValueText.text = _materialValues[_materialValueIndex[index]].ToString();
	}

	string GetResourceName(int index)
	{
		return _allResourceInventoryIndicies[index].name;
	}

	string GetResourceType(int index)
	{
		if (5 <= index && index <= 8)
			return "Pickaxe";
		if (index == 9)
			return "Axe";
		if (index == 10)
			return "Shovel";
		if (11 <= index && index <= 14)
			return "Sword";
		return "Anything";
	}

	void StartFightEventHandler()
	{
		if ((_materialValues[11] == 0 || _materialValues[12] == 0 || _materialValues[13] == 0 || _materialValues[14] == 0) && !(_materialValues[11] >= 1 || _materialValues[12] >= 1 || _materialValues[13] >= 1 || _materialValues[14] >= 1))
		{
			Debug.LogFormat("[Minecraft Survival #{0}] Fight did not start because you have no sword!", _modID);
			_resourceUntilFight = rand.Range(2, 7);
			return;
		}
        if (_dragonFightStarted)
		{
			foreach (KMSelectable km in _resourceButtons)
			{
				km.GetComponent<Renderer>().enabled = false;
				if (km.Highlight.gameObject.activeSelf)
				{
					km.Highlight.gameObject.SetActive(false);
				}
			}
			foreach (KMSelectable km in _actionButtons)
			{
				km.GetComponent<Renderer>().enabled = false;
				if (km.Highlight.gameObject.activeSelf)
				{
					km.Highlight.gameObject.SetActive(false);
				}
			}
			if (_materialValues[14] >= 1)
			{
				_actionButtonMeshes[3].material = _swordMaterials[3];
			}
			else if (_materialValues[13] >= 1)
			{
				_actionButtonMeshes[3].material = _swordMaterials[2];
			}
			else if (_materialValues[12] >= 1)
			{
				_actionButtonMeshes[3].material = _swordMaterials[1];
			}
			else if (_materialValues[11] >= 1)
			{
				_actionButtonMeshes[3].material = _swordMaterials[0];
			}
			_actionButtons[3].GetComponent<Renderer>().enabled = true;
			_actionButtons[4].GetComponent<Renderer>().enabled = true;
			_actionButtons[3].Highlight.gameObject.SetActive(true);
			_actionButtons[4].Highlight.gameObject.SetActive(true);
			_fightStarted = true;
			for (int i = 0; i <= 9; i++)
			{
				_fullHunger[i].enabled = false;
				_emptyHunger[i].enabled = false;
			}
			if (_playerHealth != 0)
			{
				_playerHealth = 0;
			}
			_mobIndicator.material = _mobMaterials[9];
			_mobIndicator.enabled = true;
			_gMonsterIndex = 10;
			_gMonsterName = _monsterIndex[10];
			_gMonsterDamage = _monsterDamage[10];
			_gMonsterHealth = _monsterHealth[10];
			_mobHealthIndicator.text = _gMonsterHealth.ToString();
            StartCoroutine(UpHearts());
			Debug.LogFormat("[Minecraft Survival #{0}] The monster you are fighting is {1} and it has {2} health and {3} damage.", _modID, _monsterIndex[_gMonsterIndex], _monsterHealth[_gMonsterIndex], _monsterDamage[_gMonsterIndex]);
			return;
		}
		string mob = "";
		string[] mobs;
		switch (_dimensionIndex)
		{
			case 0:
				mobs = _monsterTable[0];
				mob = mobs[rand.Range(0, mobs.Length)];
				break;
			case 1:
				mobs = _monsterTable[1];
				mob = mobs[rand.Range(0, mobs.Length)];
				break;
			case 2:
				mobs = _monsterTable[2];
				mob = mobs[rand.Range(0, mobs.Length)];
				break;
			default:
				Debug.LogFormat("[Minecraft Survival #{0}] Unable to create a monster fight.");
				break;
		}
		switch (mob)
		{
			case "Zombie":
				int rnd = rand.Range(0, 3);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("zombie1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("zombie2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("zombie3", transform);
				_mobIndicator.material = _mobMaterials[0];
				_gMonsterIndex = 0;
				break;
			case "Skeleton":
				rnd = rand.Range(0, 3);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("skeleton1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("skeleton2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("skeleton3", transform);
				_mobIndicator.material = _mobMaterials[1];
				_gMonsterIndex = 1;
				break;
			case "Spider":
				rnd = rand.Range(0, 4);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("spider1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("spider2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("spider3", transform);
				else if (rnd == 3)
					bAudio.PlaySoundAtTransform("spider4", transform);
				_mobIndicator.material = _mobMaterials[2];
				_gMonsterIndex = 2;
				break;
			case "Creeper":
				bAudio.PlaySoundAtTransform("creeper", transform);
				_mobIndicator.material = _mobMaterials[3];
				_gMonsterIndex = 3;
				break;
			case "Slime":
				rnd = rand.Range(0, 4);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("slime1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("slime2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("slime3", transform);
				else if (rnd == 3)
					bAudio.PlaySoundAtTransform("slime4", transform);
				_mobIndicator.material = _mobMaterials[4];
				_gMonsterIndex = 4;
				break;
			case "Pigman":
				rnd = rand.Range(0, 4);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("zpig1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("zpig2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("zpig3", transform);
				else if (rnd == 3)
					bAudio.PlaySoundAtTransform("zpig4", transform);
				_mobIndicator.material = _mobMaterials[0];
				_gMonsterIndex = 5;
				break;
			case "Blaze":
				rnd = rand.Range(0, 4);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("blaze1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("blaze2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("blaze3", transform);
				else if (rnd == 3)
					bAudio.PlaySoundAtTransform("blaze4", transform);
				_mobIndicator.material = _mobMaterials[5];
				_gMonsterIndex = 6;
				break;
			case "Wither Skeleton":
				rnd = rand.Range(0, 3);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("wskel1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("wskel2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("wskel3", transform);
				_mobIndicator.material = _mobMaterials[6];
				_gMonsterIndex = 7;
				break;
			case "Enderman":
				rnd = rand.Range(0, 5);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("enderman1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("enderman2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("enderman3", transform);
				else if (rnd == 3)
					bAudio.PlaySoundAtTransform("enderman4", transform);
				else if (rnd == 4)
					bAudio.PlaySoundAtTransform("enderman5", transform);
				_mobIndicator.material = _mobMaterials[7];
				_gMonsterIndex = 8;
				break;
			case "Shulker":
				rnd = rand.Range(0, 7);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("shulker1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("shulker2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("shulker3", transform);
				else if (rnd == 3)
					bAudio.PlaySoundAtTransform("shulker4", transform);
				else if (rnd == 4)
					bAudio.PlaySoundAtTransform("shulker5", transform);
				else if (rnd == 5)
					bAudio.PlaySoundAtTransform("shulker6", transform);
				else if (rnd == 6)
					bAudio.PlaySoundAtTransform("shulker7", transform);
				_mobIndicator.material = _mobMaterials[8];
				_gMonsterIndex = 9;
				break;
			default:
				Debug.LogFormat("[Minecraft Survival #{0}] Unable to find monster informaton.");
				break;
		}
		_mobIndicator.enabled = true;
        Debug.LogFormat("[Minecraft Survival #{0}] The monster you are fighting is {1} and it has {2} health and {3} damage.", _modID, _monsterIndex[_gMonsterIndex], _monsterHealth[_gMonsterIndex], _monsterDamage[_gMonsterIndex]);

		foreach (KMSelectable km in _resourceButtons)
		{
			km.GetComponent<Renderer>().enabled = false;
			if (km.Highlight.gameObject.activeSelf)
			{
				km.Highlight.gameObject.SetActive(false);
			}
		}

		foreach (KMSelectable km in _actionButtons)
		{
			km.GetComponent<Renderer>().enabled = false;
			if (km.Highlight.gameObject.activeSelf)
			{
				km.Highlight.gameObject.SetActive(false);
			}
		}

		if (_materialValues[14] >= 1)
		{
			_actionButtonMeshes[3].material = _swordMaterials[3];
		}
		else if (_materialValues[13] >= 1)
		{
			_actionButtonMeshes[3].material = _swordMaterials[2];
		}
		else if (_materialValues[12] >= 1)
		{
			_actionButtonMeshes[3].material = _swordMaterials[1];
		}
		else if (_materialValues[11] >= 1)
		{
			_actionButtonMeshes[3].material = _swordMaterials[0];
		}
		_actionButtons[3].GetComponent<Renderer>().enabled = true;
		_actionButtons[4].GetComponent<Renderer>().enabled = true;
		_actionButtons[3].Highlight.gameObject.SetActive(true);
		_actionButtons[4].Highlight.gameObject.SetActive(true);
		_fightStarted = true;
		_gMonsterName = _monsterIndex[_gMonsterIndex];
		_gMonsterHealth = _monsterHealth[_gMonsterIndex];
		_gMonsterDamage = _monsterDamage[_gMonsterIndex];
		_mobHealthIndicator.text = _gMonsterHealth.ToString();
		if (_playerHealth != 0)
		{
			_playerHealth = 0;
		}
		for (int i = 0; i <= 9; i++)
		{
			_fullHunger[i].enabled = false;
			_emptyHunger[i].enabled = false;
		}
		StartCoroutine(UpHearts());
	}

	void FightEventHandler()
	{
		if (_playerDamage != 20)
		{
			if (_materialValues[14] >= 1)
			{
				_playerDamage = _swordDamages[3];
			}
			else if (_materialValues[13] >= 1)
			{
				_playerDamage = _swordDamages[2];
			}
			else if (_materialValues[12] >= 1)
			{
				_playerDamage = _swordDamages[1];
			}
			else if (_materialValues[11] >= 1)
			{
				_playerDamage = _swordDamages[0];
			}
		}
		if (_playerProc == 0)
		{
			Debug.Log(_materialValues[16]);
			if (_materialValues[16] >= 1)
			{
				_playerProc = _armorProc[1];
			}
			else if (_materialValues[15] >= 1)
			{
				_playerProc = _armorProc[0];
			}
			else
			{
				_playerProc = 0;
			}
		}
		_gMonsterHealth -= _playerDamage;
		Debug.LogFormat("[Minecraft Survival #{0}] You attacked the {1} for {2} damage and now has {3} health remaining.", _modID, _gMonsterName, _playerDamage, _gMonsterHealth < 0 ? 0 : _gMonsterHealth);
		_mobHealthIndicator.text = _gMonsterHealth < 0 ? 0.ToString() : _gMonsterHealth.ToString();
		if (_dragonFightStarted && _gMonsterHealth <= 0)
		{
			Debug.LogFormat("[Minecraft Survival #{0}] Dragon defeated. Go click on the egg!", _modID);
			bAudio.PlaySoundAtTransform("end", transform);
			_dragonDefeated = true;
			_dragonFightStarted = false;
            _fightStarted = false;
            _mobIndicator.enabled = false;
			_mobHealthIndicator.text = "";
			_playerProc = 0;
			_playerDamage = -1;
			UpdateModule();
			_materialValues[41]++;
			return;
		}
		if (_gMonsterHealth <= 0)
		{
			_fightStarted = false;
			_fightReset = true;
            _resourceUntilFight = rand.Range(6, 13);
            _mobIndicator.enabled = false;
			_mobHealthIndicator.text = "";
			_playerProc = 0;
			_playerDamage = -1;
			UpdateModule();
			int drop = GiveMobDrop();
			Debug.LogFormat("[Minecraft Survival #{0}] The monster has been defeated. It dropped {1}. Resume gathering! Number of resources gathered till next fight: {2}.", _modID, drop > 1 ? drop + " items" : "an item", _resourceUntilFight);
			return;
		}
		PlayHurtSound();
		int rnd = -1;
		int monsterChance = !(_gMonsterName.Equals("Ender Dragon")) ? rand.Range(1, 5) : rand.Range(1, 26);
		if (monsterChance == 1 && _gMonsterName.Equals("Ender Dragon"))
		{
			_playerHealth -= (_gMonsterDamage - _playerProc) < 0 ? 0 : (_gMonsterDamage - _playerProc);
			if (_playerHealth <= 0)
			{
				_fightStarted = false;
				_fightReset = true;
				_resourceUntilFight = rand.Range(6, 13);
                _mobIndicator.enabled = false;
				_mobHealthIndicator.text = "";
				_playerProc = 0;
				_playerDamage = -1;
				_dragonFightStarted = false;
				UpdateModule();
				Debug.LogFormat("[Minecraft Survival #{0}] You died by a {1}. Strike!", _modID, _gMonsterName);
				GetComponent<KMBombModule>().HandleStrike();
				return;
			}
			rnd = rand.Range(0, 3);
			if (rnd == 0)
				bAudio.PlaySoundAtTransform("hit1", transform);
			else if (rnd == 1)
				bAudio.PlaySoundAtTransform("hit2", transform);
			else if (rnd == 2)
				bAudio.PlaySoundAtTransform("hit3", transform);
			Debug.LogFormat("[Minecraft Survival #{0}] You have been attacked by the {1} for {2} damage and now have {3} health remaining.", _modID, _gMonsterName, (_gMonsterDamage - _playerProc), _playerHealth < 0 ? 0 : _playerHealth);
			UpdateHearts();
			return;
		}
		if (monsterChance.EqualsAny(1, 2) && !(_gMonsterName.Equals("Creeper")))
		{
			_playerHealth -= (_gMonsterDamage - _playerProc) < 0 ? 0 : (_gMonsterDamage - _playerProc);
			if (_playerHealth <= 0)
			{
				_fightStarted = false;
				_fightReset = true;
				_resourceUntilFight = rand.Range(6, 13);
                _mobIndicator.enabled = false;
				_mobHealthIndicator.text = "";
				_playerProc = 0;
				_playerDamage = -1;
				UpdateModule();
				Debug.LogFormat("[Minecraft Survival #{0}] You died by a {1}. Strike!", _modID, _gMonsterName);
				GetComponent<KMBombModule>().HandleStrike();
				return;
			}
			rnd = rand.Range(0, 3);
			if (rnd == 0)
				bAudio.PlaySoundAtTransform("hit1", transform);
			else if (rnd == 1)
				bAudio.PlaySoundAtTransform("hit2", transform);
			else if (rnd == 2)
				bAudio.PlaySoundAtTransform("hit3", transform);
			Debug.LogFormat("[Minecraft Survival #{0}] You have been attacked by the {1} for {2} damage and now have {3} health remaining.", _modID, _gMonsterName, (_gMonsterDamage - _playerProc), _playerHealth < 0 ? 0 : _playerHealth);
			UpdateHearts();
			return;
		}
		else if (monsterChance == 1 && _gMonsterName.Equals("Creeper"))
		{
			_playerHealth -= (_gMonsterDamage - _playerProc) < 0 ? 0 : (_gMonsterDamage - _playerProc);
			if (_playerHealth <= 0)
			{
				_fightStarted = false;
				_fightReset = true;
				_resourceUntilFight = rand.Range(6, 13);
                _mobIndicator.enabled = false;
				_mobHealthIndicator.text = "";
				_playerProc = 0;
				_playerDamage = -1;
				UpdateModule();
				Debug.LogFormat("[Minecraft Survival #{0}] A Creeper blew you up. You died, aw man. Strike!", _modID);
				GetComponent<KMBombModule>().HandleStrike();
				return;
			}
			rnd = rand.Range(0, 3);
			if (rnd == 0)
				bAudio.PlaySoundAtTransform("hit1", transform);
			else if (rnd == 1)
				bAudio.PlaySoundAtTransform("hit2", transform);
			else if (rnd == 2)
				bAudio.PlaySoundAtTransform("hit3", transform);
			Debug.LogFormat("[Minecraft Survival #{0}] You have been attacked by the {1} for {2} damage and now have {3} health remaining.", _modID, _gMonsterName, (_gMonsterDamage - _playerProc), _playerHealth);
			UpdateHearts();
			return;
		}
	}

	void EatEventHandler()
	{
		int rnd = -1;
		if (_fightStarted || _dragonFightStarted)
		{
			if (_playerHealth == 20) { return; }
			if (_materialValues[4] >= 1)
			{
				rnd = rand.Range(0, 3);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("eat1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("eat2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("eat3", transform);
				_materialValues[4]--;
				_playerHealth = (_playerHealth + 7) > 20 ? 20 : (_playerHealth + 7);
				UpdateHearts();
			}
		}
		else
		{
			if (_playerHunger == 10) { return; }
			if (_materialValues[4] >= 1)
			{
				rnd = rand.Range(0, 3);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("eat1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("eat2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("eat3", transform);
				_materialValues[4]--;
				_playerHunger = (_playerHunger + 4) > 10 ? 10 : (_playerHunger + 4);
				UpdateHunger();
			}
		}
	}

	void StartTheDamnDragonFightAlready()
	{
		_dragonFightStarted = true;
		int rnd = rand.Range(0, 4);
		if (rnd == 0)
			bAudio.PlaySoundAtTransform("growl1", transform);
		else if (rnd == 1)
			bAudio.PlaySoundAtTransform("growl2", transform);
		else if (rnd == 2)
			bAudio.PlaySoundAtTransform("growl3", transform);
		else if (rnd == 3)
			bAudio.PlaySoundAtTransform("growl4", transform);
		StartFightEventHandler();
	}

	void UpdateHearts()
	{
		switch (_playerHealth)
		{
			case 0:
				foreach (SpriteRenderer sr in _fullHearts)
				{
					sr.enabled = false;
				}
				foreach (SpriteRenderer sr in _halfHearts)
				{
					sr.enabled = false;
				}
				foreach (SpriteRenderer sr in _emptyHearts)
				{
					sr.enabled = true;
				}
				break;
			case 1:
				for (int i = 0; i <= 9; i++)
				{
					_fullHearts[i].enabled = false;
					if (i == 0) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; }
					if (i == 0) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 2:
				for (int i = 0; i <= 9; i++)
				{
					if (i == 0) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					_halfHearts[i].enabled = false;
					if (i == 0) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 3:
				for (int i = 0; i <= 9; i++)
				{
					if (i == 0) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					if (i == 1) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; };
					if (i.EqualsAny(0, 1)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 4:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					_halfHearts[i].enabled = false;
					if (i.EqualsAny(0, 1)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 5:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					if (i == 2) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; };
					if (i.EqualsAny(0, 1, 2)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 6:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					_halfHearts[i].enabled = false;
					if (i.EqualsAny(0, 1, 2)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 7:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					if (i == 3) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; };
					if (i.EqualsAny(0, 1, 2, 3)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 8:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					_halfHearts[i].enabled = false;
					if (i.EqualsAny(0, 1, 2, 3)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 9:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					if (i == 4) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; };
					if (i.EqualsAny(0, 1, 2, 3, 4)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 10:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					_halfHearts[i].enabled = false;
					if (i.EqualsAny(0, 1, 2, 3, 4)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 11:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					if (i == 5) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; };
					if (i.EqualsAny(0, 1, 2, 3, 4, 5)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 12:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					_halfHearts[i].enabled = false;
					if (i.EqualsAny(0, 1, 2, 3, 4, 5)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 13:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					if (i == 6) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; };
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 14:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					_halfHearts[i].enabled = false;
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 15:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					if (i == 7) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; };
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 16:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					_halfHearts[i].enabled = false;
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 17:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					if (i == 8) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; };
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7, 8)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 18:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7, 8)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					_halfHearts[i].enabled = false;
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7, 8)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 19:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7, 8)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					if (i == 9) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; };
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7, 8, 9)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 20:
				foreach (SpriteRenderer sr in _fullHearts)
				{
					sr.enabled = true;
				}
				foreach (SpriteRenderer sr in _halfHearts)
				{
					sr.enabled = false;
				}
				foreach (SpriteRenderer sr in _emptyHearts)
				{
					sr.enabled = false;
				}
				break;
			default:
				Debug.LogFormat("[Minecraft Survival #{0}] Unable to update player health indicator.", _modID);
				break;
		}
	}

	void UpdateHunger()
	{
		if (_fightReset)
		{
			_playerHunger = 10;
			_fightReset = false;
		}
		switch (_playerHunger)
		{
			case 0:
				foreach (SpriteRenderer sr in _fullHunger)
				{
					sr.enabled = false;
				}
				foreach (SpriteRenderer sr in _emptyHunger)
				{
					sr.enabled = true;
				}
				break;
			case 1:
				for (int i = 0; i <= 9; i++)
				{
					if (i == 0) { _fullHunger[i].enabled = true; _emptyHunger[i].enabled = false; continue; }
					_fullHunger[i].enabled = false;
					_emptyHunger[i].enabled = true;
				}
				break;
			case 2:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1)) { _fullHunger[i].enabled = true; _emptyHunger[i].enabled = false; continue; }
					_fullHunger[i].enabled = false;
					_emptyHunger[i].enabled = true;
				}
				break;
			case 3:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2)) { _fullHunger[i].enabled = true; _emptyHunger[i].enabled = false; continue; }
					_fullHunger[i].enabled = false;
					_emptyHunger[i].enabled = true;
				}
				break;
			case 4:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3)) { _fullHunger[i].enabled = true; _emptyHunger[i].enabled = false; continue; }
					_fullHunger[i].enabled = false;
					_emptyHunger[i].enabled = true;
				}
				break;
			case 5:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4)) { _fullHunger[i].enabled = true; _emptyHunger[i].enabled = false; continue; }
					_fullHunger[i].enabled = false;
					_emptyHunger[i].enabled = true;
				}
				break;
			case 6:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5)) { _fullHunger[i].enabled = true; _emptyHunger[i].enabled = false; continue; }
					_fullHunger[i].enabled = false;
					_emptyHunger[i].enabled = true;
				}
				break;
			case 7:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6)) { _fullHunger[i].enabled = true; _emptyHunger[i].enabled = false; continue; }
					_fullHunger[i].enabled = false;
					_emptyHunger[i].enabled = true;
				}
				break;
			case 8:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7)) { _fullHunger[i].enabled = true; _emptyHunger[i].enabled = false; continue; }
					_fullHunger[i].enabled = false;
					_emptyHunger[i].enabled = true;
				}
				break;
			case 9:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7, 8)) { _fullHunger[i].enabled = true; _emptyHunger[i].enabled = false; continue; }
					_fullHunger[i].enabled = false;
					_emptyHunger[i].enabled = true;
				}
				break;
			case 10:
				foreach (SpriteRenderer sr in _fullHunger)
				{
					sr.enabled = true;
				}
				foreach (SpriteRenderer sr in _emptyHunger)
				{
					sr.enabled = false;
				}
				break;
			default:
				Debug.LogFormat("[Minecraft Survival #{0}] Unable to update hunger bar.", _modID);
				break;
		}
	}

	void UpdateModule()
	{
		_actionButtons[3].GetComponent<Renderer>().enabled = false;
		_actionButtons[4].GetComponent<Renderer>().enabled = false;
		_actionButtons[3].Highlight.gameObject.SetActive(false);
		_actionButtons[4].Highlight.gameObject.SetActive(false);
		int i = 0;
		foreach (KMSelectable action in _actionButtons)
		{
			if (i.EqualsAny(3, 4, 7)) { i++; continue; }
			action.GetComponent<Renderer>().enabled = true;
			action.Highlight.gameObject.SetActive(true);
			i++;
		}
		i = 0;
		switch (_dimensionIndex)
		{
			case 0:
				foreach (KMSelectable resource in _resourceButtons)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7)) { resource.GetComponent<Renderer>().enabled = true; resource.Highlight.gameObject.SetActive(true); i++; continue; }
					resource.GetComponent<Renderer>().enabled = false; resource.Highlight.gameObject.SetActive(false);
					i++;
				}
				if (!_dragonDefeated)
				{
					_actionButtons[7].GetComponent<Renderer>().enabled = false; _actionButtons[7].Highlight.gameObject.SetActive(false);
				}
				_dimensionIndex = 0;
				_moduleMeshes[0].material = _dimensionMaterials[0];
				_moduleMeshes[1].material = _dimensionMaterials[1];
				break;
			case 1:
				foreach (KMSelectable resource in _resourceButtons)
				{
					if (i.EqualsAny(8, 9, 10, 11)) { resource.GetComponent<Renderer>().enabled = true; resource.Highlight.gameObject.SetActive(true); i++; continue; }
					resource.GetComponent<Renderer>().enabled = false; resource.Highlight.gameObject.SetActive(false);
					i++;
				}
				if (!_dragonDefeated)
				{
					_actionButtons[7].GetComponent<Renderer>().enabled = false; _actionButtons[7].Highlight.gameObject.SetActive(false);
				}
				_dimensionIndex = 1;
				_moduleMeshes[0].material = _dimensionMaterials[2];
				_moduleMeshes[1].material = _dimensionMaterials[3];
				break;
			case 2:
				foreach (KMSelectable resource in _resourceButtons)
				{
					if (i.EqualsAny(12, 13, 14, 15)) { resource.GetComponent<Renderer>().enabled = true; resource.Highlight.gameObject.SetActive(true); i++; continue; }
					resource.GetComponent<Renderer>().enabled = false; resource.Highlight.gameObject.SetActive(false);
					i++;
				}
				if (!_dragonDefeated)
				{
					_actionButtons[7].GetComponent<Renderer>().enabled = true; _actionButtons[7].Highlight.gameObject.SetActive(true);
				}
				_dimensionIndex = 2;
				_moduleMeshes[0].material = _dimensionMaterials[4];
				_moduleMeshes[1].material = _dimensionMaterials[5];
				break;
			default:
				Debug.LogFormat("[Minecraft Survival #{0}] Unable to update module.", _modID);
				break;
		}
		for (int g = 0; g <= 9; g++)
		{
			_emptyHearts[g].enabled = false;
			_halfHearts[g].enabled = false;
			_fullHearts[g].enabled = false;
		}
		UpdateHunger();
	}

	void PlayHurtSound()
	{
		switch (_gMonsterName)
		{
			case "Zombie":
				int rnd = rand.Range(0, 2);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("zombiehurt1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("zombiehurt2", transform);
				break;
			case "Skeleton":
				rnd = rand.Range(0, 4);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("skeletonhurt1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("skeletonhurt2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("skeletonhurt3", transform);
				else if (rnd == 3)
					bAudio.PlaySoundAtTransform("skeletonhurt4", transform);
				break;
			case "Spider":
				rnd = rand.Range(0, 4);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("spider1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("spider2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("spider3", transform);
				else if (rnd == 3)
					bAudio.PlaySoundAtTransform("spider4", transform);
				break;
			case "Creeper":
				rnd = rand.Range(0, 4);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("creeperhurt1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("creeperhurt2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("creeperhurt3", transform);
				else if (rnd == 3)
					bAudio.PlaySoundAtTransform("creeperhurt4", transform);
				break;
			case "Slime":
				rnd = rand.Range(0, 4);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("slime1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("slime2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("slime3", transform);
				else if (rnd == 3)
					bAudio.PlaySoundAtTransform("slime4", transform);
				break;
			case "Pigman":
				rnd = rand.Range(0, 2);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("zpighurt1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("zpighurt2", transform);
				break;
			case "Blaze":
				rnd = rand.Range(0, 4);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("blazehurt1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("blazehurt2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("blazehurt3", transform);
				else if (rnd == 3)
					bAudio.PlaySoundAtTransform("blazehurt4", transform);
				break;
			case "Wither Skeleton":
				rnd = rand.Range(0, 4);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("wskelhurt1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("wskelhurt2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("wskelhurt3", transform);
				else if (rnd == 3)
					bAudio.PlaySoundAtTransform("wskelhurt4", transform);
				break;
			case "Enderman":
				rnd = rand.Range(0, 4);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("endermanhurt1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("endermanhurt2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("endermanhurt3", transform);
				else if (rnd == 3)
					bAudio.PlaySoundAtTransform("endermanhurt4", transform);
				break;
			case "Ender Dragon":
				rnd = rand.Range(0, 4);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("enderdragonhurt1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("enderdragonhurt2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("enderdragonhurt3", transform);
				else if (rnd == 3)
					bAudio.PlaySoundAtTransform("enderdragonhurt4", transform);
				break;
			case "Shulker":
				rnd = rand.Range(0, 4);
				if (rnd == 0)
					bAudio.PlaySoundAtTransform("shulkerhurt1", transform);
				else if (rnd == 1)
					bAudio.PlaySoundAtTransform("shulkerhurt2", transform);
				else if (rnd == 2)
					bAudio.PlaySoundAtTransform("shulkerhurt3", transform);
				else if (rnd == 3)
					bAudio.PlaySoundAtTransform("shulkerhurt4", transform);
				break;
		}
	}

	int GiveMobDrop()
	{
		int num = 0;
		switch (_gMonsterName)
		{
			case "Zombie":
				bAudio.PlaySoundAtTransform("zombiedeath", transform);
				if (_materialValues[35] >= 64 || _materialValues[35] + 1 >= 64) { _materialValues[35] = 64; break; }
				_materialValues[35]++;
				num = 1;
				break;
			case "Skeleton":
				bAudio.PlaySoundAtTransform("skeletondeath", transform);
				int rnd = rand.Range(2, 4);
				if (_materialValues[36] >= 64 || _materialValues[36] + rnd >= 64) { _materialValues[36] = 64; break; }
				_materialValues[36] += rnd;
				num = rnd;
				break;
			case "Spider":
				bAudio.PlaySoundAtTransform("spiderdeath", transform);
				if (_materialValues[37] >= 64 || _materialValues[37] + 2 >= 64) { _materialValues[37] = 64; break; }
				_materialValues[37] += 2;
				num = 2;
				break;
			case "Creeper":
				bAudio.PlaySoundAtTransform("creeperdeath", transform);
				if (_materialValues[38] >= 64 || _materialValues[38] + 1 >= 64) { _materialValues[38] = 64; break; }
				_materialValues[38]++;
				num = 1;
				break;
			case "Slime":
				int rnd2 = rand.Range(0, 4);
				if (rnd2 == 0)
					bAudio.PlaySoundAtTransform("slime1", transform);
				else if (rnd2 == 1)
					bAudio.PlaySoundAtTransform("slime2", transform);
				else if (rnd2 == 2)
					bAudio.PlaySoundAtTransform("slime3", transform);
				else if (rnd2 == 3)
					bAudio.PlaySoundAtTransform("slime4", transform);
				rnd = rand.Range(2, 4);
				if (_materialValues[39] >= 64 || _materialValues[39] + rnd >= 64) { _materialValues[39] = 64; break; }
				_materialValues[39] += rnd;
				num = rnd;
				break;
			case "Pigman":
				bAudio.PlaySoundAtTransform("zpigdeath", transform);
				if (_materialValues[35] >= 64 || _materialValues[35] + 2 >= 64) { _materialValues[35] = 64; break; }
				_materialValues[35] += 2;
				num = 2;
				break;
			case "Blaze":
				bAudio.PlaySoundAtTransform("blazedeath", transform);
				if (_materialValues[32] >= 64 || _materialValues[32] + 2 >= 64) { _materialValues[32] = 64; break; }
				_materialValues[32] += 2;
				num = 2;
				break;
			case "Wither Skeleton":
				rnd2 = rand.Range(0, 2);
				if (rnd2 == 0)
					bAudio.PlaySoundAtTransform("wskeldeath1", transform);
				else if (rnd2 == 1)
					bAudio.PlaySoundAtTransform("wskeldeath2", transform);
				rnd = rand.Range(2, 5);
				if (_materialValues[20] >= 64 || _materialValues[20] + rnd >= 64) { _materialValues[20] = 64; break; }
				_materialValues[20] += rnd;
				num = rnd;
				break;
			case "Enderman":
				bAudio.PlaySoundAtTransform("endermandeath", transform);
				if (_materialValues[33] >= 64 || _materialValues[33] + 2 >= 64) { _materialValues[33] = 64; break; }
				_materialValues[33] += 2;
				num = 2;
				break;
			case "Shulker":
				rnd2 = rand.Range(0, 4);
				if (rnd2 == 0)
					bAudio.PlaySoundAtTransform("shulkerdeath1", transform);
				else if (rnd2 == 1)
					bAudio.PlaySoundAtTransform("shulkerdeath2", transform);
				else if (rnd2 == 2)
					bAudio.PlaySoundAtTransform("shulkerdeath3", transform);
				else if (rnd2 == 3)
					bAudio.PlaySoundAtTransform("shulkerdeath4", transform);
				if (_materialValues[40] >= 64 || _materialValues[40] + 1 >= 64) { _materialValues[40] = 64; break; }
				_materialValues[40]++;
				num = 1;
				break;
			default:
				Debug.LogFormat("[Minecraft Survival #{0}] Unable to find monster informaton.", _modID);
				break;
		}
		return num;
	}

	void SolveModule()
	{
		foreach (KMSelectable sr in _actionButtons)
		{
			sr.GetComponent<Renderer>().enabled = false;
			sr.Highlight.gameObject.SetActive(false);
		}
		foreach (KMSelectable sr in _resourceButtons)
		{
			sr.GetComponent<Renderer>().enabled = false;
			sr.Highlight.gameObject.SetActive(false);
		}
		foreach (KMSelectable sr in _allResourceInventoryIndicies)
		{
			sr.GetComponent<Renderer>().enabled = false;
			sr.Highlight.gameObject.SetActive(false);
		}
		for (int i = 0; i <= 9; i++)
		{
			_emptyHunger[i].enabled = false;
			_fullHunger[i].enabled = false;
			_emptyHearts[i].enabled = false;
			_halfHearts[i].enabled = false;
			_fullHearts[i].enabled = false;
		}
		_materialValueText.text = "";
		_mobHealthIndicator.text = "";
		_moduleMeshes[0].material = _dimensionMaterials[8];
		_moduleMeshes[1].material = _dimensionMaterials[8];
		_modSolved = true;
	}

	IEnumerator OpenInventory()
	{
		_isAnimating = true;
		bAudio.PlaySoundAtTransform("chestopen", transform);
		_moduleMeshes[0].material = _dimensionMaterials[6];
		_moduleMeshes[1].material = _dimensionMaterials[7];
		_materialValueText.text = "";
		for (int i = 0; i <= 9; i++)
		{
			_fullHunger[i].enabled = false;
			_emptyHunger[i].enabled = false;
		}
		foreach (KMSelectable action in _actionButtons)
		{
			action.GetComponent<Renderer>().enabled = false;
			action.Highlight.gameObject.SetActive(false);
		}
		foreach (KMSelectable resource in _resourceButtons)
		{
			resource.GetComponent<Renderer>().enabled = false;
			resource.Highlight.gameObject.SetActive(false);
		}
		foreach (KMSelectable inv in _allResourceInventoryIndicies)
		{
			inv.GetComponent<Renderer>().enabled = true;
			yield return new WaitForSeconds(0.015f);
		}
		foreach (KMSelectable inv in _allResourceInventoryIndicies)
		{
			inv.Highlight.gameObject.SetActive(true);
		}
		_isAnimating = false;
		_materialValueText.text = "-";
		_materialValueText.transform.localPosition = _invTextPos;
		_actionButtons[5].GetComponent<Renderer>().enabled = true;
		_actionButtons[5].Highlight.gameObject.SetActive(true);
		yield break;
	}

	IEnumerator CloseInventory()
	{
		KMSelectable[] kma = new KMSelectable[44];
		_allResourceInventoryIndicies.CopyTo(kma, 0);
		_materialValueText.text = "";
		_isAnimating = true;
		bAudio.PlaySoundAtTransform("chestclosed", transform);
		_actionButtons[5].GetComponent<Renderer>().enabled = false;
		_actionButtons[5].Highlight.gameObject.SetActive(false);
		foreach (KMSelectable inv in kma.Reverse())
		{
			inv.Highlight.gameObject.SetActive(false);
		}
		foreach (KMSelectable inv in kma.Reverse())
		{
			inv.GetComponent<Renderer>().enabled = false;
			yield return new WaitForSeconds(0.015f);
		}
		_materialValueText.transform.localPosition = _resourceTextPos;
		UpdateModule();
		UpdateHunger();
		_isAnimating = false;
		yield break;
	}

	IEnumerator UpdateHighlights()
	{
		yield return new WaitForSeconds(0.7f);
		int i = 0;
		foreach (KMSelectable action in _actionButtons)
		{
			if (i.EqualsAny(0, 1, 2, 5, 6)) { i++; continue; }
			action.Highlight.gameObject.SetActive(false);
			i++;
		}
		i = 0;
		foreach (KMSelectable resource in _resourceButtons)
		{
			if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7)) { i++; continue; }
			resource.Highlight.gameObject.SetActive(false);
			i++;
		}
		foreach (KMSelectable inv in _allResourceInventoryIndicies)
		{
			inv.Highlight.gameObject.SetActive(false);
		}
		_materialValues[9] = 1;
		_materialValues[4] = 10;
		yield break;
	}

	IEnumerator UpHearts()
	{
		_isAnimating = true;
		while (_playerHealth <= 20)
		{
			foreach (SpriteRenderer sr in _regenHearts)
			{
				sr.enabled = true;
			}
			yield return new WaitForSeconds(0.02f);
			foreach (SpriteRenderer sr in _regenHearts)
			{
				sr.enabled = false;
			}
			UpdateHearts();
			yield return new WaitForSeconds(0.02f);
			_playerHealth++;
		}
		_isAnimating = false;
		_playerHealth = 20;
		yield break;
	}

	IEnumerator UpHunger()
	{
		_isAnimating = true;
		while (_playerHunger <= 10)
		{
			UpdateHunger();
			_playerHunger++;
			yield return new WaitForSeconds(0.1f);
		}
		_isAnimating = false;
		_playerHunger = 10;
		yield break;
	}
	}

	//twitch plays
#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} inventory/inv [Toggles being in and out of the inventory] | !{0} amount/amt <item> [See the amount of the specified item in the inventory] | !{0} craft (#) <item> [Crafts the specified item in the inventory (optionally '#' times)] | !{0} dimension/dim <dimension> [Changes to the specified dimension] | !{0} gather (#) <item> [Gathers the specified item (optionally '#' times)] | !{0} eat [Eats a cooked beef] | !{0} attack (#) [Attacks a mob in a fight (optionally '#' times)] | !{0} items [Gives a list of all gatherable and craftable items] | !{0} dragon [Enter the Ender Dragon battle] | !{0} egg [Clicks the dragon egg in the inventory]";
	#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
	{
		if (Regex.IsMatch(command, @"^\s*items\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) 
		{
			yield return "sendtochat The possible gatherable items are " + _resourceButtons.Select(x => x.name).Distinct().Join(", ") + ".";
			yield return "sendtochat The possible craftable items are " + _inventoryButtons.Select(x => x.name).Join(", ") + ".";
			yield break;
		}
		if (Regex.IsMatch(command, @"^\s*egg\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			if (_fightStarted)
			{
				yield return "sendtochaterror Cannot click the dragon egg while you are in combat!";
			}
			else if (!_inInventory)
			{
				yield return "sendtochaterror Cannot click the dragon egg if not in the inventory!";
			}
			else if (_isAnimating)
			{
				yield return "sendtochaterror Cannot click the dragon egg while the module is animating!";
			}
			else
			{
				_allResourceInventoryIndicies[41].OnInteract();
			}
			yield break;
		}
		if (Regex.IsMatch(command, @"^\s*eat\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			if (_inInventory)
			{
				yield return "sendtochaterror Cannot eat a cooked beef while in the inventory!";
			}
			else if (_isAnimating)
			{
				yield return "sendtochaterror Cannot eat a cooked beef while the module is animating!";
			}
			else
			{
				if (_fightStarted)
					_actionButtons[4].OnInteract();
                else if (_playerHunger != 10)
                {
                    _actionButtons[6].OnInteract();
                    yield return new WaitForSeconds(1f);
                    _actionButtons[6].OnHighlightEnded();
                }
			}
			yield break;
		}
		if (Regex.IsMatch(command, @"^\s*dragon\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			if (_fightStarted)
			{
				yield return "sendtochaterror Cannot enter the Ender Dragon battle while you are in combat!";
			}
			else if (_dimensionIndex != 2)
			{
				yield return "sendtochaterror Cannot enter the Ender Dragon battle if not in The End!";
			}
			else if (_dragonDefeated)
			{
				yield return "sendtochaterror Cannot enter the Ender Dragon battle since the Ender Dragon is dead!";
			}
			else if (_isAnimating)
			{
				yield return "sendtochaterror Cannot enter the Ender Dragon battle while the module is animating!";
			}
			else
			{
				_actionButtons[7].OnInteract();
			}
			yield break;
		}
		if (Regex.IsMatch(command, @"^\s*inventory\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(command, @"^\s*inv\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			if (_fightStarted)
			{
				yield return "sendtochaterror The inventory cannot be toggled while you are in combat!";
			}
			else if (_isAnimating)
			{
				yield return "sendtochaterror The inventory cannot be toggled while the module is animating!";
			}
			else
			{
				_actionButtons[5].OnInteract();
			}
			yield break;
		}
		string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*attack\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (!_fightStarted)
            {
                yield return "sendtochaterror Cannot attack a mob if you are not in combat!";
            }
            else if (_isAnimating)
            {
                yield return "sendtochaterror Cannot attack a mob while the module is animating!";
            }
            else
            {
                if (parameters.Length == 2)
                {
                    int temp = 0;
                    if (!int.TryParse(parameters[1], out temp))
                    {
                        yield return "sendtochaterror!f The specified number of times to attack '" + parameters[1] + "' is invalid!";
                        yield break;
                    }
                    if (temp < 1)
                    {
                        yield return "sendtochaterror The specified number of times to attack '" + parameters[1] + "' is less than 1!";
                        yield break;
                    }
                    for (int i = 0; i < temp; i++)
                    {
                        _actionButtons[3].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                        if (!_fightStarted && i != temp - 1)
                        {
                            yield break;
                        }
                    }
                }
                else if (parameters.Length == 1)
                {
                    _actionButtons[3].OnInteract();
                }
            }
            yield break;
        }
        if (Regex.IsMatch(parameters[0], @"^\s*amount\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(parameters[0], @"^\s*amt\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			if (_fightStarted)
			{
				yield return "sendtochaterror Cannot view the amount of an item while you are in combat!";
			}
			else if (!_inInventory)
			{
				yield return "sendtochaterror Cannot view the amount of an item if not in the inventory!";
			}
			else if (_isAnimating)
			{
				yield return "sendtochaterror Cannot view the amount of an item while the module is animating!";
			}
			else
			{
				if (parameters.Length == 1)
				{
					yield return "sendtochaterror Please specify the item you wish to see the amount of!";
				}
				else
				{
					string origitem = "";
					string item = "";
					if (parameters[0].ToLowerInvariant() == "amt")
						item = command.Substring(4);
					else
						item = command.Substring(7);
					origitem = item;
					item = item.ToLower();
					for (int i = 0; i < _allResourceInventoryIndicies.Length; i++)
					{
						if (item.Equals(_allResourceInventoryIndicies[i].name.ToLower()))
						{
							_allResourceInventoryIndicies[i].OnHighlight();
							yield return new WaitForSeconds(1f);
							_allResourceInventoryIndicies[i].OnHighlightEnded();
							yield break;
						}
					}
					yield return "sendtochaterror!f The specified item '" + origitem + "' does not exist!";
				}
			}
			yield break;
		}
		if (Regex.IsMatch(parameters[0], @"^\s*craft\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			if (_fightStarted)
			{
				yield return "sendtochaterror Cannot craft an item while you are in combat!";
			}
			else if (!_inInventory)
			{
				yield return "sendtochaterror Cannot craft an item if not in the inventory!";
			}
			else if (_isAnimating)
			{
				yield return "sendtochaterror Cannot craft an item while the module is animating!";
			}
			else
			{
				if (parameters.Length == 1)
				{
					yield return "sendtochaterror Please specify the item you wish to craft!";
				}
				else
				{
					int temp = -1;
					string origitem = "";
					string item = "";
					if (int.TryParse(parameters[1], out temp))
					{
						if (temp < 0)
						{
							yield return "sendtochaterror The specified number of times to craft '" + temp + "' is less than 1!";
							yield break;
						}
						if (6 + ("" + temp).Length == command.Length) 
						{
							yield return "sendtochaterror Please specify the item you wish to craft!";
							yield break;
						}
						origitem = command.Substring(7 + ("" + temp).Length);
						item = command.Substring(7 + ("" + temp).Length);
					}
					else
					{
						temp = 1;
						origitem = command.Substring(6);
						item = command.Substring(6);
					}
					item = item.ToLower();
					for (int i = 0; i < _allResourceInventoryIndicies.Length; i++)
					{
						if (item.Equals(GetResourceName(i).ToLower()))
						{
							for (int j = 0; j < _inventoryButtons.Length; j++)
							{
								if (item.Equals(_inventoryButtons[j].name.ToLower()))
								{
									for (int k = 0; k < temp; k++)
									{
										_inventoryButtons[j].OnInteract();
										yield return new WaitForSeconds(0.1f);
									}
                                    yield return new WaitForSeconds(0.9f);
                                    _inventoryButtons[j].OnHighlightEnded();
                                    yield break;
								}
							}
							yield return "sendtochaterror The specified item '" + origitem + "' cannot be crafted!";
							yield break;
						}
					}
					yield return "sendtochaterror!f The specified item '" + origitem + "' does not exist!";
				}
			}
			yield break;
		}
		if (Regex.IsMatch(parameters[0], @"^\s*gather\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			if (_fightStarted)
			{
				yield return "sendtochaterror Cannot gather an item while you are in combat!";
			}
			else if (_inInventory)
			{
				yield return "sendtochaterror Cannot gather an item if in the inventory!";
			}
			else if (_isAnimating)
			{
				yield return "sendtochaterror Cannot gather an item while the module is animating!";
			}
			else
			{
				if (parameters.Length == 1)
				{
					yield return "sendtochaterror Please specify the item you wish to gather!";
				}
				else
				{
					int temp = -1;
					string origitem = "";
					string item = "";
					if (int.TryParse(parameters[1], out temp))
					{
						if (temp < 0)
						{
							yield return "sendtochaterror The specified number of times to gather '" + temp + "' is less than 1!";
							yield break;
						}
						if (7 + ("" + temp).Length == command.Length)
						{
							yield return "sendtochaterror Please specify the item you wish to gather!";
							yield break;
						}
						origitem = command.Substring(8 + ("" + temp).Length);
						item = command.Substring(8 + ("" + temp).Length);
					}
					else
					{
						temp = 1;
						origitem = command.Substring(7);
						item = command.Substring(7);
					}
					item = item.ToLower();
					for (int i = 0; i < _allResourceInventoryIndicies.Length; i++)
					{
						if (item.Equals("coal ore"))
							item = "coal";
						else if (item.Equals("diamond ore"))
							item = "diamonds";
						else if (item.Equals("gravel"))
							item = "flint";
						else if (item.Equals("glowstone"))
							item = "glowstone dust";
						if (item.Equals(GetResourceName(i).ToLower()))
						{
							for (int j = 0; j < _resourceButtons.Length; j++)
							{
								if (item.Equals("coal"))
									item = "coal ore";
								else if (item.Equals("diamonds"))
									item = "diamond ore";
								else if (item.Equals("flint"))
									item = "gravel";
								else if (item.Equals("glowstone dust"))
									item = "glowstone";
								if (item.Equals(_resourceButtons[j].name.ToLower()))
								{
									if (j == 6 && _dimensionIndex == 2)
										j = 13;
									if (_dimensionIndex != 0 && j < 8)
									{
										yield return "sendtochaterror You must be in " + (j == 6 ? "The Overworld/The End" : "The Overworld") + " to gather this item!";
										yield break;
									}
									else if (_dimensionIndex != 1 && j > 7 && j < 12)
									{
										yield return "sendtochaterror You must be in The Nether to gather this item!";
										yield break;
									}
									else if (_dimensionIndex != 2 && j > 11)
									{
										yield return "sendtochaterror You must be in The End to gather this item!";
										yield break;
									}
									for (int k = 0; k < temp; k++)
									{
										_resourceButtons[j].OnInteract();
										yield return new WaitForSeconds(0.1f);
										if (_fightStarted && k != temp - 1)
										{
                                            _resourceButtons[j].OnHighlightEnded();
                                            yield return "sendtochat Halted gathering of items due to a mob encounter! Successfully gathered " + (k + 1) + " times before being halted!";
                                            yield break;
										}
									}
                                    yield return new WaitForSeconds(0.9f);
                                    _resourceButtons[j].OnHighlightEnded();
                                    yield break;
								}
							}
							yield return "sendtochaterror The specified item '" + origitem + "' cannot be gathered!";
							yield break;
						}
					}
					yield return "sendtochaterror!f The specified item '" + origitem + "' does not exist!";
				}
			}
			yield break;
		}
		if (Regex.IsMatch(parameters[0], @"^\s*dimension\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(parameters[0], @"^\s*dim\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			if (_inInventory)
			{
				yield return "sendtochaterror Cannot attempt to change dimensions while in the inventory!";
			}
			else if (_fightStarted)
			{
				yield return "sendtochaterror Cannot attempt to change dimensions while in combat!";
			}
			else
			{
				if (parameters.Length == 1)
				{
					yield return "sendtochaterror Please specify the dimension you wish to attempt to change to!";
				}
				else
				{
					string origdim = "";
					string dim = "";
					if (parameters[0] == "dim")
						dim = command.Substring(4);
					else
						dim = command.Substring(10);
					origdim = dim;
					dim = dim.ToLower();
					string[] dimensions = new string[] { "the overworld", "the nether", "the end" };
					if (dimensions.Contains(dim))
					{
						if (Array.IndexOf(dimensions, dim) == _dimensionIndex)
						{
							yield return "sendtochaterror You are already in this dimension!";
							yield break;
						}
						_actionButtons[Array.IndexOf(dimensions, dim)].OnInteract();
						yield break;
					}
					yield return "sendtochaterror!f The specified dimension '" + origdim + "' does not exist!";
				}
			}
			yield break;
		}
	}

	IEnumerator TwitchHandleForcedSolve()
	{
        if (!_startedModule)
        {
            if (_inInventory)
            {
                _actionButtons[5].OnInteract();
                while (_isAnimating) { yield return true; }
            }
            for (int i = 0; i < 2; i++)
            {
                _resourceButtons[0].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            _actionButtons[5].OnInteract();
            while (_isAnimating) { yield return true; }
            for (int i = 0; i < 3; i++)
            {
                _inventoryButtons[0].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            for (int i = 0; i < 2; i++)
            {
                _inventoryButtons[1].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            _inventoryButtons[5].OnInteract();
            yield return new WaitForSeconds(0.1f);
            _inventoryButtons[10].OnInteract();
            yield return new WaitForSeconds(0.1f);
            _actionButtons[5].OnInteract();
            while (_isAnimating) { yield return true; }
            while (_materialValues[19] < 16)
            {
                if (_playerHunger == 1)
                {
                    _actionButtons[6].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                _resourceButtons[1].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            if (_playerHunger == 1)
            {
                _actionButtons[6].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            _resourceButtons[5].OnInteract();
            yield return new WaitForSeconds(0.1f);
            while (_materialValues[20] < 15)
            {
                if (_playerHunger == 1)
                {
                    _actionButtons[6].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                _resourceButtons[2].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            _actionButtons[5].OnInteract();
            while (_isAnimating) { yield return true; }
            _inventoryButtons[2].OnInteract();
            yield return new WaitForSeconds(0.1f);
            _inventoryButtons[6].OnInteract();
            yield return new WaitForSeconds(0.1f);
            _actionButtons[5].OnInteract();
            while (_isAnimating) { yield return true; }
            while (_materialValues[21] < 11)
            {
                if (_playerHunger == 1)
                {
                    _actionButtons[6].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                _resourceButtons[3].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            _actionButtons[5].OnInteract();
            while (_isAnimating) { yield return true; }
            while (_materialValues[3] < 11)
            {
                _inventoryButtons[3].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            _inventoryButtons[7].OnInteract();
            yield return new WaitForSeconds(0.1f);
            _inventoryButtons[19].OnInteract();
            yield return new WaitForSeconds(0.1f);
            _actionButtons[5].OnInteract();
            while (_isAnimating) { yield return true; }
            while (_materialValues[22] < 36)
            {
                if (_playerHunger == 1)
                {
                    _actionButtons[6].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                _resourceButtons[4].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            _actionButtons[5].OnInteract();
            while (_isAnimating) { yield return true; }
            _inventoryButtons[8].OnInteract();
            yield return new WaitForSeconds(0.1f);
            _inventoryButtons[16].OnInteract();
            yield return new WaitForSeconds(0.1f);
            _inventoryButtons[11].OnInteract();
            yield return new WaitForSeconds(0.1f);
            _inventoryButtons[12].OnInteract();
            yield return new WaitForSeconds(0.1f);
            _inventoryButtons[13].OnInteract();
            yield return new WaitForSeconds(0.1f);
            _inventoryButtons[14].OnInteract();
            yield return new WaitForSeconds(0.1f);
            _actionButtons[5].OnInteract();
            while (_isAnimating) { yield return true; }
            while (_materialValues[24] < 10)
            {
                if (_playerHunger == 1)
                {
                    if (_materialValues[4] == 0)
                    {
                        _actionButtons[5].OnInteract();
                        while (_isAnimating) { yield return true; }
                        _inventoryButtons[4].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                        _actionButtons[5].OnInteract();
                        while (_isAnimating) { yield return true; }
                    }
                    _actionButtons[6].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                _resourceButtons[7].OnInteract();
                yield return new WaitForSeconds(0.1f);
                if (_fightStarted)
                {
                    _resourceButtons[7].OnHighlightEnded();
                    while (_isAnimating) { yield return true; }
                    while (_gMonsterHealth > 0)
                    {
                        _actionButtons[3].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                    }
                }
            }
            _actionButtons[5].OnInteract();
            while (_isAnimating) { yield return true; }
            while (_materialValues[4] < 10)
            {
                _inventoryButtons[4].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            _actionButtons[5].OnInteract();
            while (_isAnimating) { yield return true; }
            while (_materialValues[25] < 14)
            {
                if (_playerHunger == 1)
                {
                    _actionButtons[6].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                _resourceButtons[6].OnInteract();
                yield return new WaitForSeconds(0.1f);
                if (_fightStarted)
                {
                    _resourceButtons[6].OnHighlightEnded();
                    while (_isAnimating) { yield return true; }
                    while (_gMonsterHealth > 0)
                    {
                        _actionButtons[3].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                    }
                }
            }
            _actionButtons[1].OnInteract();
            yield return new WaitForSeconds(0.1f);
            while (_materialValues[32] < 6 || _materialValues[33] < 12)
            {
                if (_materialValues[4] == 1)
                {
                    _actionButtons[0].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                    _resourceButtons[2].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                    if (_fightStarted)
                    {
                        _resourceButtons[2].OnHighlightEnded();
                        while (_isAnimating) { yield return true; }
                        while (_gMonsterHealth > 0)
                        {
                            _actionButtons[3].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                    _resourceButtons[7].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                    if (_fightStarted)
                    {
                        _resourceButtons[7].OnHighlightEnded();
                        while (_isAnimating) { yield return true; }
                        while (_gMonsterHealth > 0)
                        {
                            _actionButtons[3].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                    _actionButtons[5].OnInteract();
                    while (_isAnimating) { yield return true; }
                    _inventoryButtons[4].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                    _actionButtons[5].OnInteract();
                    while (_isAnimating) { yield return true; }
                    _actionButtons[1].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                if (_playerHunger == 1)
                {
                    _actionButtons[6].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                int lastpress = 0;
                if (_materialValues[34] < 63)
                {
                    lastpress = 8;
                    _resourceButtons[8].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                else if (_materialValues[26] < 63)
                {
                    lastpress = 9;
                    _resourceButtons[9].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                else if (_materialValues[27] < 63)
                {
                    lastpress = 10;
                    _resourceButtons[10].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                else if (_materialValues[28] < 63)
                {
                    lastpress = 11;
                    _resourceButtons[11].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    lastpress = rand.Range(8, 12);
                    _resourceButtons[lastpress].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                if (_fightStarted)
                {
                    _resourceButtons[lastpress].OnHighlightEnded();
                    while (_isAnimating) { yield return true; }
                    while (_gMonsterHealth > 0)
                    {
                        _actionButtons[3].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                    }
                }
            }
            _actionButtons[5].OnInteract();
            while (_isAnimating) { yield return true; }
            for (int i = 0; i < 6; i++)
            {
                _inventoryButtons[17].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            for (int i = 0; i < 12; i++)
            {
                _inventoryButtons[18].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            _actionButtons[5].OnInteract();
            while (_isAnimating) { yield return true; }
            _actionButtons[2].OnInteract();
            yield return new WaitForSeconds(0.1f);
            _actionButtons[7].OnInteract();
            yield return new WaitForSeconds(0.1f);
            while (_isAnimating) { yield return true; }
            while (_gMonsterHealth > 0)
            {
                _actionButtons[3].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            _actionButtons[5].OnInteract();
            while (_isAnimating) { yield return true; }
            GetComponent<KMSelectable>().Children[63].OnInteract();
        }
        else
        {
            SolveModule();
            GetComponent<KMBombModule>().HandlePass();
        }
    }
}
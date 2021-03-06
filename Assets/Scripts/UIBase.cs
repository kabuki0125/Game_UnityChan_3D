﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UIのベースクラス.
/// このスクリプトがアタッチされている以下のスクリプトを簡単にとってこれるようになる.
/// </summary>
public class UIBase : MonoBehaviour
{   
	private Dictionary<Type, Dictionary<string, object>>	Scripts
	{
		get{
			if( this.m_scripts == null ){
				this.m_scripts	= new Dictionary<Type, Dictionary<string, object>>();
			}
			return m_scripts;
		}
	}
	private Dictionary<Type, Dictionary<string, object>> m_scripts;

	/// <summary>
	/// 子階層にあるすべてのスクリプトから該当名のものを取得する.
	/// </summary>
	public T GetScript<T>(string key) where T : Component
	{
		if(!this.Scripts.ContainsKey(typeof(T)) ){
			this.UpdateScriptList<T>();
		}
		var	tbl	= this.Scripts[typeof(T)];
		return tbl[key] as T;
	}
	private void UpdateScriptList<T>() where T : Component
	{
		if( this.Scripts.ContainsKey(typeof(T)) ){
			this.Scripts[typeof(T)].Clear();
		}
		this.Scripts[typeof(T)]	= this.GetScriptList(typeof(T));
	}
    
    /// <summary>
    /// 指定した型のオブジェクトを全て取得する.
    /// </summary>
	public Dictionary<string, object> GetScriptList(Type type)
	{
		var	tbl	= new Dictionary<string, object>();
		
		foreach(var i in this.GetComponentsInChildren(type, true)){
			tbl[i.name]	= i;
			
			if( i.transform.parent != null ){
				tbl[i.transform.parent.name + "/" + i.name]	= i;
			}
		}
		return tbl;
	}
}

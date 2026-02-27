package com.server;

import java.util.List;

import java.util.Map;

import com.entity.Fangwuxinxi;

public interface FangwuxinxiServer {

  public int add(Fangwuxinxi po);

  public int update(Fangwuxinxi po);
  
  
  
  public int delete(int id);

  public List<Fangwuxinxi> getAll(Map<String,Object> map);
  public List<Fangwuxinxi> getsyfangwuxinxi1(Map<String,Object> map);
  public List<Fangwuxinxi> getsyfangwuxinxi2(Map<String,Object> map);
  public List<Fangwuxinxi> getsyfangwuxinxi3(Map<String,Object> map);
  public Fangwuxinxi quchongFangwuxinxi(Map<String, Object> acount);

  public Fangwuxinxi getById( int id);

  public List<Fangwuxinxi> getByPage(Map<String, Object> map);

  public int getCount(Map<String,Object> map);

  public List<Fangwuxinxi> select(Map<String, Object> map);
}
//	所有List

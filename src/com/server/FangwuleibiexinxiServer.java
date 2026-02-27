package com.server;

import java.util.List;

import java.util.Map;

import com.entity.Fangwuleibiexinxi;

public interface FangwuleibiexinxiServer {

  public int add(Fangwuleibiexinxi po);

  public int update(Fangwuleibiexinxi po);
  
  
  
  public int delete(int id);

  public List<Fangwuleibiexinxi> getAll(Map<String,Object> map);
  public List<Fangwuleibiexinxi> getsyfangwuleibiexinxi1(Map<String,Object> map);
  public List<Fangwuleibiexinxi> getsyfangwuleibiexinxi2(Map<String,Object> map);
  public List<Fangwuleibiexinxi> getsyfangwuleibiexinxi3(Map<String,Object> map);
  public Fangwuleibiexinxi quchongFangwuleibiexinxi(Map<String, Object> acount);

  public Fangwuleibiexinxi getById( int id);

  public List<Fangwuleibiexinxi> getByPage(Map<String, Object> map);

  public int getCount(Map<String,Object> map);

  public List<Fangwuleibiexinxi> select(Map<String, Object> map);
}
//	所有List

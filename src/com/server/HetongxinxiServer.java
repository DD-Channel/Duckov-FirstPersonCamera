package com.server;

import java.util.List;

import java.util.Map;

import com.entity.Hetongxinxi;

public interface HetongxinxiServer {

  public int add(Hetongxinxi po);

  public int update(Hetongxinxi po);
  
  
  
  public int delete(int id);

  public List<Hetongxinxi> getAll(Map<String,Object> map);
  public List<Hetongxinxi> getsyhetongxinxi1(Map<String,Object> map);
  public List<Hetongxinxi> getsyhetongxinxi2(Map<String,Object> map);
  public List<Hetongxinxi> getsyhetongxinxi3(Map<String,Object> map);
  public Hetongxinxi quchongHetongxinxi(Map<String, Object> acount);

  public Hetongxinxi getById( int id);

  public List<Hetongxinxi> getByPage(Map<String, Object> map);

  public int getCount(Map<String,Object> map);

  public List<Hetongxinxi> select(Map<String, Object> map);
}
//	所有List

package com.dao;

import java.util.List;
import java.util.Map;

import com.entity.Fangwuleibiexinxi;

public interface FangwuleibiexinxiMapper {
    int deleteByPrimaryKey(Integer id);

    int insert(Fangwuleibiexinxi record);

    int insertSelective(Fangwuleibiexinxi record);

    Fangwuleibiexinxi selectByPrimaryKey(Integer id);

    int updateByPrimaryKeySelective(Fangwuleibiexinxi record);
	
    int updateByPrimaryKey(Fangwuleibiexinxi record);
	public Fangwuleibiexinxi quchongFangwuleibiexinxi(Map<String, Object> fangwuleibie);
	public List<Fangwuleibiexinxi> getAll(Map<String, Object> map);
	public List<Fangwuleibiexinxi> getsyfangwuleibiexinxi1(Map<String, Object> map);
	public List<Fangwuleibiexinxi> getsyfangwuleibiexinxi3(Map<String, Object> map);
	public List<Fangwuleibiexinxi> getsyfangwuleibiexinxi2(Map<String, Object> map);
	public int getCount(Map<String, Object> po);
	public List<Fangwuleibiexinxi> getByPage(Map<String, Object> map);
	public List<Fangwuleibiexinxi> select(Map<String, Object> map);
//	所有List
}


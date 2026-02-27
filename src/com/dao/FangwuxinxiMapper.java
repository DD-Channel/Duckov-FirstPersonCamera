package com.dao;

import java.util.List;
import java.util.Map;

import com.entity.Fangwuxinxi;

public interface FangwuxinxiMapper {
    int deleteByPrimaryKey(Integer id);

    int insert(Fangwuxinxi record);

    int insertSelective(Fangwuxinxi record);

    Fangwuxinxi selectByPrimaryKey(Integer id);

    int updateByPrimaryKeySelective(Fangwuxinxi record);
	
    int updateByPrimaryKey(Fangwuxinxi record);
	public Fangwuxinxi quchongFangwuxinxi(Map<String, Object> fangwubianhao);
	public List<Fangwuxinxi> getAll(Map<String, Object> map);
	public List<Fangwuxinxi> getsyfangwuxinxi1(Map<String, Object> map);
	public List<Fangwuxinxi> getsyfangwuxinxi3(Map<String, Object> map);
	public List<Fangwuxinxi> getsyfangwuxinxi2(Map<String, Object> map);
	public int getCount(Map<String, Object> po);
	public List<Fangwuxinxi> getByPage(Map<String, Object> map);
	public List<Fangwuxinxi> select(Map<String, Object> map);
//	所有List
}


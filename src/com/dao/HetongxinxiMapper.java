package com.dao;

import java.util.List;
import java.util.Map;

import com.entity.Hetongxinxi;

public interface HetongxinxiMapper {
    int deleteByPrimaryKey(Integer id);

    int insert(Hetongxinxi record);

    int insertSelective(Hetongxinxi record);

    Hetongxinxi selectByPrimaryKey(Integer id);

    int updateByPrimaryKeySelective(Hetongxinxi record);
	
    int updateByPrimaryKey(Hetongxinxi record);
	public Hetongxinxi quchongHetongxinxi(Map<String, Object> fangwubianhao);
	public List<Hetongxinxi> getAll(Map<String, Object> map);
	public List<Hetongxinxi> getsyhetongxinxi1(Map<String, Object> map);
	public List<Hetongxinxi> getsyhetongxinxi3(Map<String, Object> map);
	public List<Hetongxinxi> getsyhetongxinxi2(Map<String, Object> map);
	public int getCount(Map<String, Object> po);
	public List<Hetongxinxi> getByPage(Map<String, Object> map);
	public List<Hetongxinxi> select(Map<String, Object> map);
//	所有List
}


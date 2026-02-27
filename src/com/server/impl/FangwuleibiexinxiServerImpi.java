package com.server.impl;

import java.util.List;

import java.util.Map;

import javax.annotation.Resource;

import org.springframework.stereotype.Service;

import com.dao.FangwuleibiexinxiMapper;
import com.entity.Fangwuleibiexinxi;
import com.server.FangwuleibiexinxiServer;
@Service
public class FangwuleibiexinxiServerImpi implements FangwuleibiexinxiServer {
   @Resource
   private FangwuleibiexinxiMapper gdao;
	@Override
	public int add(Fangwuleibiexinxi po) {
		return gdao.insert(po);
	}

	@Override
	public int update(Fangwuleibiexinxi po) {
		return gdao.updateByPrimaryKeySelective(po);
	}

	
	
	@Override
	public int delete(int id) {
		return gdao.deleteByPrimaryKey(id);
	}

	@Override
	public List<Fangwuleibiexinxi> getAll(Map<String, Object> map) {
		return gdao.getAll(map);
	}
	
	public List<Fangwuleibiexinxi> getsyfangwuleibiexinxi1(Map<String, Object> map) {
		return gdao.getsyfangwuleibiexinxi1(map);
	}
	public List<Fangwuleibiexinxi> getsyfangwuleibiexinxi2(Map<String, Object> map) {
		return gdao.getsyfangwuleibiexinxi2(map);
	}
	public List<Fangwuleibiexinxi> getsyfangwuleibiexinxi3(Map<String, Object> map) {
		return gdao.getsyfangwuleibiexinxi3(map);
	}
	
	@Override
	public Fangwuleibiexinxi quchongFangwuleibiexinxi(Map<String, Object> account) {
		return gdao.quchongFangwuleibiexinxi(account);
	}

	@Override
	public List<Fangwuleibiexinxi> getByPage(Map<String, Object> map) {
		return gdao.getByPage(map);
	}

	@Override
	public int getCount(Map<String, Object> map) {
		return gdao.getCount(map);
	}

	@Override
	public List<Fangwuleibiexinxi> select(Map<String, Object> map) {
		return gdao.select(map);
	}

	@Override
	public Fangwuleibiexinxi getById(int id) {
		return gdao.selectByPrimaryKey(id);
	}

}


package com.server.impl;

import java.util.List;

import java.util.Map;

import javax.annotation.Resource;

import org.springframework.stereotype.Service;

import com.dao.FangwuxinxiMapper;
import com.entity.Fangwuxinxi;
import com.server.FangwuxinxiServer;
@Service
public class FangwuxinxiServerImpi implements FangwuxinxiServer {
   @Resource
   private FangwuxinxiMapper gdao;
	@Override
	public int add(Fangwuxinxi po) {
		return gdao.insert(po);
	}

	@Override
	public int update(Fangwuxinxi po) {
		return gdao.updateByPrimaryKeySelective(po);
	}

	
	
	@Override
	public int delete(int id) {
		return gdao.deleteByPrimaryKey(id);
	}

	@Override
	public List<Fangwuxinxi> getAll(Map<String, Object> map) {
		return gdao.getAll(map);
	}
	
	public List<Fangwuxinxi> getsyfangwuxinxi1(Map<String, Object> map) {
		return gdao.getsyfangwuxinxi1(map);
	}
	public List<Fangwuxinxi> getsyfangwuxinxi2(Map<String, Object> map) {
		return gdao.getsyfangwuxinxi2(map);
	}
	public List<Fangwuxinxi> getsyfangwuxinxi3(Map<String, Object> map) {
		return gdao.getsyfangwuxinxi3(map);
	}
	
	@Override
	public Fangwuxinxi quchongFangwuxinxi(Map<String, Object> account) {
		return gdao.quchongFangwuxinxi(account);
	}

	@Override
	public List<Fangwuxinxi> getByPage(Map<String, Object> map) {
		return gdao.getByPage(map);
	}

	@Override
	public int getCount(Map<String, Object> map) {
		return gdao.getCount(map);
	}

	@Override
	public List<Fangwuxinxi> select(Map<String, Object> map) {
		return gdao.select(map);
	}

	@Override
	public Fangwuxinxi getById(int id) {
		return gdao.selectByPrimaryKey(id);
	}

}


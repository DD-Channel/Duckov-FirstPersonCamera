package com.server.impl;

import java.util.List;

import java.util.Map;

import javax.annotation.Resource;

import org.springframework.stereotype.Service;

import com.dao.HetongxinxiMapper;
import com.entity.Hetongxinxi;
import com.server.HetongxinxiServer;
@Service
public class HetongxinxiServerImpi implements HetongxinxiServer {
   @Resource
   private HetongxinxiMapper gdao;
	@Override
	public int add(Hetongxinxi po) {
		return gdao.insert(po);
	}

	@Override
	public int update(Hetongxinxi po) {
		return gdao.updateByPrimaryKeySelective(po);
	}

	
	
	@Override
	public int delete(int id) {
		return gdao.deleteByPrimaryKey(id);
	}

	@Override
	public List<Hetongxinxi> getAll(Map<String, Object> map) {
		return gdao.getAll(map);
	}
	
	public List<Hetongxinxi> getsyhetongxinxi1(Map<String, Object> map) {
		return gdao.getsyhetongxinxi1(map);
	}
	public List<Hetongxinxi> getsyhetongxinxi2(Map<String, Object> map) {
		return gdao.getsyhetongxinxi2(map);
	}
	public List<Hetongxinxi> getsyhetongxinxi3(Map<String, Object> map) {
		return gdao.getsyhetongxinxi3(map);
	}
	
	@Override
	public Hetongxinxi quchongHetongxinxi(Map<String, Object> account) {
		return gdao.quchongHetongxinxi(account);
	}

	@Override
	public List<Hetongxinxi> getByPage(Map<String, Object> map) {
		return gdao.getByPage(map);
	}

	@Override
	public int getCount(Map<String, Object> map) {
		return gdao.getCount(map);
	}

	@Override
	public List<Hetongxinxi> select(Map<String, Object> map) {
		return gdao.select(map);
	}

	@Override
	public Hetongxinxi getById(int id) {
		return gdao.selectByPrimaryKey(id);
	}

}


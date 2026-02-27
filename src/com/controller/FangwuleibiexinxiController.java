package com.controller;

import java.io.File;
import java.io.IOException;

import java.sql.Timestamp;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.io.PrintWriter;

import javax.annotation.Resource;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;
import javax.servlet.http.HttpSession;

import org.springframework.stereotype.Controller;
import org.springframework.ui.ModelMap;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.multipart.MultipartFile;

import com.entity.Fangwuleibiexinxi;
import com.server.FangwuleibiexinxiServer;
import com.util.PageBean;
import net.sf.json.JSONObject;
import com.util.db;
import java.sql.SQLException;
import java.sql.*;
@Controller
public class FangwuleibiexinxiController {
	@Resource
	private FangwuleibiexinxiServer fangwuleibiexinxiService;

	@RequestMapping("addFangwuleibiexinxi.do")
	public String addFangwuleibiexinxi(HttpServletRequest request,Fangwuleibiexinxi fangwuleibiexinxi,HttpSession session) throws SQLException{
		Timestamp time=new Timestamp(System.currentTimeMillis());
		
		fangwuleibiexinxi.setAddtime(time.toString().substring(0, 19));
		fangwuleibiexinxiService.add(fangwuleibiexinxi);
		db dbo = new db();
		
		//kuabiaogaizhi
		session.setAttribute("backxx", "添加成功");session.setAttribute("backurl", request.getHeader("Referer"));return "redirect:postback.jsp";
		
	}
	@RequestMapping("addFangwuleibiexinxiqt.do")
	public String addFangwuleibiexinxiqt(HttpServletRequest request,Fangwuleibiexinxi fangwuleibiexinxi,HttpSession session) throws SQLException{
		Timestamp time=new Timestamp(System.currentTimeMillis());
		
		fangwuleibiexinxi.setAddtime(time.toString().substring(0, 19));
		fangwuleibiexinxiService.add(fangwuleibiexinxi);
		db dbo = new db();
		
		//kuabiaogaizhi
		session.setAttribute("backxx", "添加成功");
		session.setAttribute("backurl", request.getHeader("Referer"));
		return "redirect:postback.jsp";
	}
 
//	处理编辑
	@RequestMapping("doUpdateFangwuleibiexinxi.do")
	public String doUpdateFangwuleibiexinxi(int id,ModelMap map,Fangwuleibiexinxi fangwuleibiexinxi){
		fangwuleibiexinxi=fangwuleibiexinxiService.getById(id);
		map.put("fangwuleibiexinxi", fangwuleibiexinxi);
		return "fangwuleibiexinxi_updt";
	}
	
	
	
	
//	后台详细
	@RequestMapping("fangwuleibiexinxiDetail.do")
	public String fangwuleibiexinxiDetail(int id,ModelMap map,Fangwuleibiexinxi fangwuleibiexinxi){
		fangwuleibiexinxi=fangwuleibiexinxiService.getById(id);
		map.put("fangwuleibiexinxi", fangwuleibiexinxi);
		return "fangwuleibiexinxi_detail";
	}
//	前台详细
	@RequestMapping("fwlbxxDetail.do")
	public String fwlbxxDetail(int id,ModelMap map,Fangwuleibiexinxi fangwuleibiexinxi){
		fangwuleibiexinxi=fangwuleibiexinxiService.getById(id);
		map.put("fangwuleibiexinxi", fangwuleibiexinxi);
		return "fangwuleibiexinxidetail";
	}
//	
	@RequestMapping("updateFangwuleibiexinxi.do")
	public String updateFangwuleibiexinxi(int id,ModelMap map,Fangwuleibiexinxi fangwuleibiexinxi,HttpServletRequest request,HttpSession session){
		fangwuleibiexinxiService.update(fangwuleibiexinxi);
		
		session.setAttribute("backxx", "修改成功");session.setAttribute("backurl", request.getHeader("Referer"));return "redirect:postback.jsp";
	}

//	分页查询
	@RequestMapping("fangwuleibiexinxiList.do")
	public String fangwuleibiexinxiList(@RequestParam(value="page",required=false)String page,
			ModelMap map,HttpSession session,Fangwuleibiexinxi fangwuleibiexinxi, String fangwuleibie){
		if(page==null||page.equals("")){
			page="1";
		}
		PageBean pageBean=new PageBean(Integer.parseInt(page), 8);
		Map<String, Object> pmap=new HashMap<String,Object>();
		pmap.put("pageno", pageBean.getStart());
		pmap.put("pageSize", 8);
		
		
		if(fangwuleibie==null||fangwuleibie.equals("")){pmap.put("fangwuleibie", null);}else{pmap.put("fangwuleibie", fangwuleibie);}		
		int total=fangwuleibiexinxiService.getCount(pmap);
		pageBean.setTotal(total);
		List<Fangwuleibiexinxi> list=fangwuleibiexinxiService.getByPage(pmap);
		map.put("page", pageBean);
		map.put("list", list);
		session.setAttribute("p", 1);
		return "fangwuleibiexinxi_list";
	}
	
	
	@RequestMapping("fwlbxxList.do")
	public String fwlbxxList(@RequestParam(value="page",required=false)String page,
			ModelMap map,HttpSession session,Fangwuleibiexinxi fangwuleibiexinxi, String fangwuleibie){
		if(page==null||page.equals("")){
			page="1";
		}
		PageBean pageBean=new PageBean(Integer.parseInt(page), 8);
		Map<String, Object> pmap=new HashMap<String,Object>();
		pmap.put("pageno", pageBean.getStart());
		pmap.put("pageSize", 8);
		if(fangwuleibie==null||fangwuleibie.equals("")){pmap.put("fangwuleibie", null);}else{pmap.put("fangwuleibie", fangwuleibie);}		
		int total=fangwuleibiexinxiService.getCount(pmap);
		pageBean.setTotal(total);
		List<Fangwuleibiexinxi> list=fangwuleibiexinxiService.getByPage(pmap);
		map.put("page", pageBean);
		map.put("list", list);
		session.setAttribute("p", 1);
		return "fangwuleibiexinxilist";
	}
	@RequestMapping("fwlbxxListtp.do")
	public String fwlbxxListtp(@RequestParam(value="page",required=false)String page,
			ModelMap map,HttpSession session,Fangwuleibiexinxi fangwuleibiexinxi, String fangwuleibie){
		if(page==null||page.equals("")){
			page="1";
		}
		PageBean pageBean=new PageBean(Integer.parseInt(page), 8);
		Map<String, Object> pmap=new HashMap<String,Object>();
		pmap.put("pageno", pageBean.getStart());
		pmap.put("pageSize", 8);
		if(fangwuleibie==null||fangwuleibie.equals("")){pmap.put("fangwuleibie", null);}else{pmap.put("fangwuleibie", fangwuleibie);}		
		int total=fangwuleibiexinxiService.getCount(pmap);
		pageBean.setTotal(total);
		List<Fangwuleibiexinxi> list=fangwuleibiexinxiService.getByPage(pmap);
		map.put("page", pageBean);
		map.put("list", list);
		session.setAttribute("p", 1);
		return "fangwuleibiexinxilisttp";
	}
	
	@RequestMapping("deleteFangwuleibiexinxi.do")
	public String deleteFangwuleibiexinxi(int id,HttpServletRequest request,HttpSession session){
		fangwuleibiexinxiService.delete(id);
		session.setAttribute("backxx", "删除成功");session.setAttribute("backurl", request.getHeader("Referer"));return "redirect:postback.jsp";
	}
	
	
}

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

import com.entity.Hetongxinxi;
import com.server.HetongxinxiServer;
import com.util.PageBean;
import net.sf.json.JSONObject;
import com.util.db;
import java.sql.SQLException;
import java.sql.*;
@Controller
public class HetongxinxiController {
	@Resource
	private HetongxinxiServer hetongxinxiService;

	@RequestMapping("addHetongxinxi.do")
	public String addHetongxinxi(HttpServletRequest request,Hetongxinxi hetongxinxi,HttpSession session) throws SQLException{
		Timestamp time=new Timestamp(System.currentTimeMillis());
		
		hetongxinxi.setAddtime(time.toString().substring(0, 19));
		hetongxinxiService.add(hetongxinxi);
		db dbo = new db();
		
		//kuabiaogaizhi
		session.setAttribute("backxx", "添加成功");session.setAttribute("backurl", request.getHeader("Referer"));return "redirect:postback.jsp";
		
	}
	@RequestMapping("addHetongxinxiqt.do")
	public String addHetongxinxiqt(HttpServletRequest request,Hetongxinxi hetongxinxi,HttpSession session) throws SQLException{
		Timestamp time=new Timestamp(System.currentTimeMillis());
		
		hetongxinxi.setAddtime(time.toString().substring(0, 19));
		hetongxinxiService.add(hetongxinxi);
		db dbo = new db();
		
		//kuabiaogaizhi
		session.setAttribute("backxx", "添加成功");
		session.setAttribute("backurl", request.getHeader("Referer"));
		return "redirect:postback.jsp";
	}
 
//	处理编辑
	@RequestMapping("doUpdateHetongxinxi.do")
	public String doUpdateHetongxinxi(int id,ModelMap map,Hetongxinxi hetongxinxi){
		hetongxinxi=hetongxinxiService.getById(id);
		map.put("hetongxinxi", hetongxinxi);
		return "hetongxinxi_updt";
	}
	
	
	
	
//	后台详细
	@RequestMapping("hetongxinxiDetail.do")
	public String hetongxinxiDetail(int id,ModelMap map,Hetongxinxi hetongxinxi){
		hetongxinxi=hetongxinxiService.getById(id);
		map.put("hetongxinxi", hetongxinxi);
		return "hetongxinxi_detail";
	}
//	前台详细
	@RequestMapping("htxxDetail.do")
	public String htxxDetail(int id,ModelMap map,Hetongxinxi hetongxinxi){
		hetongxinxi=hetongxinxiService.getById(id);
		map.put("hetongxinxi", hetongxinxi);
		return "hetongxinxidetail";
	}
//	
	@RequestMapping("updateHetongxinxi.do")
	public String updateHetongxinxi(int id,ModelMap map,Hetongxinxi hetongxinxi,HttpServletRequest request,HttpSession session){
		hetongxinxiService.update(hetongxinxi);
		
		session.setAttribute("backxx", "修改成功");session.setAttribute("backurl", request.getHeader("Referer"));return "redirect:postback.jsp";
	}

//	分页查询
	@RequestMapping("hetongxinxiList.do")
	public String hetongxinxiList(@RequestParam(value="page",required=false)String page,
			ModelMap map,HttpSession session,Hetongxinxi hetongxinxi, String fangwubianhao, String fangwumingcheng, String fangwuleibie, String yuejiage, String lianxidianhua, String yonghuzhanghao, String yonghuxingming, String yonghudianhua, String zulinyuefen, String yingfujine, String hetongzhaopian, String hetongxiazai, String shengxiaoshijian1,String shengxiaoshijian2, String daoqishijian1,String daoqishijian2){
		if(page==null||page.equals("")){
			page="1";
		}
		PageBean pageBean=new PageBean(Integer.parseInt(page), 5);
		Map<String, Object> pmap=new HashMap<String,Object>();
		pmap.put("pageno", pageBean.getStart());
		pmap.put("pageSize", 5);
		
		
		if(fangwubianhao==null||fangwubianhao.equals("")){pmap.put("fangwubianhao", null);}else{pmap.put("fangwubianhao", fangwubianhao);}		if(fangwumingcheng==null||fangwumingcheng.equals("")){pmap.put("fangwumingcheng", null);}else{pmap.put("fangwumingcheng", fangwumingcheng);}		if(fangwuleibie==null||fangwuleibie.equals("")){pmap.put("fangwuleibie", null);}else{pmap.put("fangwuleibie", fangwuleibie);}		if(yuejiage==null||yuejiage.equals("")){pmap.put("yuejiage", null);}else{pmap.put("yuejiage", yuejiage);}		if(lianxidianhua==null||lianxidianhua.equals("")){pmap.put("lianxidianhua", null);}else{pmap.put("lianxidianhua", lianxidianhua);}		if(yonghuzhanghao==null||yonghuzhanghao.equals("")){pmap.put("yonghuzhanghao", null);}else{pmap.put("yonghuzhanghao", yonghuzhanghao);}		if(yonghuxingming==null||yonghuxingming.equals("")){pmap.put("yonghuxingming", null);}else{pmap.put("yonghuxingming", yonghuxingming);}		if(yonghudianhua==null||yonghudianhua.equals("")){pmap.put("yonghudianhua", null);}else{pmap.put("yonghudianhua", yonghudianhua);}		if(zulinyuefen==null||zulinyuefen.equals("")){pmap.put("zulinyuefen", null);}else{pmap.put("zulinyuefen", zulinyuefen);}		if(yingfujine==null||yingfujine.equals("")){pmap.put("yingfujine", null);}else{pmap.put("yingfujine", yingfujine);}		if(hetongzhaopian==null||hetongzhaopian.equals("")){pmap.put("hetongzhaopian", null);}else{pmap.put("hetongzhaopian", hetongzhaopian);}		if(hetongxiazai==null||hetongxiazai.equals("")){pmap.put("hetongxiazai", null);}else{pmap.put("hetongxiazai", hetongxiazai);}		if(shengxiaoshijian1==null||shengxiaoshijian1.equals("")){pmap.put("shengxiaoshijian1", null);}else{pmap.put("shengxiaoshijian1", shengxiaoshijian1);}		if(shengxiaoshijian2==null||shengxiaoshijian2.equals("")){pmap.put("shengxiaoshijian2", null);}else{pmap.put("shengxiaoshijian2", shengxiaoshijian2);}		if(daoqishijian1==null||daoqishijian1.equals("")){pmap.put("daoqishijian1", null);}else{pmap.put("daoqishijian1", daoqishijian1);}		if(daoqishijian2==null||daoqishijian2.equals("")){pmap.put("daoqishijian2", null);}else{pmap.put("daoqishijian2", daoqishijian2);}		
		int total=hetongxinxiService.getCount(pmap);
		pageBean.setTotal(total);
		List<Hetongxinxi> list=hetongxinxiService.getByPage(pmap);
		map.put("page", pageBean);
		map.put("list", list);
		session.setAttribute("p", 1);
		return "hetongxinxi_list";
	}
	@RequestMapping("hetongxinxiList2.do")
	public String hetongxinxiList2(@RequestParam(value="page",required=false)String page,
			ModelMap map,HttpSession session,Hetongxinxi hetongxinxi, String fangwubianhao, String fangwumingcheng, String fangwuleibie, String yuejiage, String lianxidianhua, String yonghuzhanghao, String yonghuxingming, String yonghudianhua, String zulinyuefen, String yingfujine, String hetongzhaopian, String hetongxiazai, String shengxiaoshijian1,String shengxiaoshijian2, String daoqishijian1,String daoqishijian2,HttpServletRequest request){
		/*if(session.getAttribute("user")==null){
			return "login";
		}*/
		if(page==null||page.equals("")){
			page="1";
		}
		PageBean pageBean=new PageBean(Integer.parseInt(page), 15);
		Map<String, Object> pmap=new HashMap<String,Object>();
		pmap.put("pageno", pageBean.getStart());
		pmap.put("pageSize", 15);
		
		pmap.put("yonghuzhanghao", (String)request.getSession().getAttribute("username"));
		if(fangwubianhao==null||fangwubianhao.equals("")){pmap.put("fangwubianhao", null);}else{pmap.put("fangwubianhao", fangwubianhao);}		if(fangwumingcheng==null||fangwumingcheng.equals("")){pmap.put("fangwumingcheng", null);}else{pmap.put("fangwumingcheng", fangwumingcheng);}		if(fangwuleibie==null||fangwuleibie.equals("")){pmap.put("fangwuleibie", null);}else{pmap.put("fangwuleibie", fangwuleibie);}		if(yuejiage==null||yuejiage.equals("")){pmap.put("yuejiage", null);}else{pmap.put("yuejiage", yuejiage);}		if(lianxidianhua==null||lianxidianhua.equals("")){pmap.put("lianxidianhua", null);}else{pmap.put("lianxidianhua", lianxidianhua);}		if(yonghuxingming==null||yonghuxingming.equals("")){pmap.put("yonghuxingming", null);}else{pmap.put("yonghuxingming", yonghuxingming);}		if(yonghudianhua==null||yonghudianhua.equals("")){pmap.put("yonghudianhua", null);}else{pmap.put("yonghudianhua", yonghudianhua);}		if(zulinyuefen==null||zulinyuefen.equals("")){pmap.put("zulinyuefen", null);}else{pmap.put("zulinyuefen", zulinyuefen);}		if(yingfujine==null||yingfujine.equals("")){pmap.put("yingfujine", null);}else{pmap.put("yingfujine", yingfujine);}		if(hetongzhaopian==null||hetongzhaopian.equals("")){pmap.put("hetongzhaopian", null);}else{pmap.put("hetongzhaopian", hetongzhaopian);}		if(hetongxiazai==null||hetongxiazai.equals("")){pmap.put("hetongxiazai", null);}else{pmap.put("hetongxiazai", hetongxiazai);}		if(shengxiaoshijian1==null||shengxiaoshijian1.equals("")){pmap.put("shengxiaoshijian1", null);}else{pmap.put("shengxiaoshijian1", shengxiaoshijian1);}		if(shengxiaoshijian2==null||shengxiaoshijian2.equals("")){pmap.put("shengxiaoshijian2", null);}else{pmap.put("shengxiaoshijian2", shengxiaoshijian2);}		if(daoqishijian1==null||daoqishijian1.equals("")){pmap.put("daoqishijian1", null);}else{pmap.put("daoqishijian1", daoqishijian1);}		if(daoqishijian2==null||daoqishijian2.equals("")){pmap.put("daoqishijian2", null);}else{pmap.put("daoqishijian2", daoqishijian2);}		
		
		int total=hetongxinxiService.getCount(pmap);
		pageBean.setTotal(total);
		List<Hetongxinxi> list=hetongxinxiService.getByPage(pmap);
		map.put("page", pageBean);
		map.put("list", list);
		session.setAttribute("p", 1);
		return "hetongxinxi_list2";
	}	
	
	@RequestMapping("htxxList.do")
	public String htxxList(@RequestParam(value="page",required=false)String page,
			ModelMap map,HttpSession session,Hetongxinxi hetongxinxi, String fangwubianhao, String fangwumingcheng, String fangwuleibie, String yuejiage, String lianxidianhua, String yonghuzhanghao, String yonghuxingming, String yonghudianhua, String zulinyuefen, String yingfujine, String hetongzhaopian, String hetongxiazai, String shengxiaoshijian1,String shengxiaoshijian2, String daoqishijian1,String daoqishijian2){
		if(page==null||page.equals("")){
			page="1";
		}
		PageBean pageBean=new PageBean(Integer.parseInt(page), 5);
		Map<String, Object> pmap=new HashMap<String,Object>();
		pmap.put("pageno", pageBean.getStart());
		pmap.put("pageSize", 5);
		if(fangwubianhao==null||fangwubianhao.equals("")){pmap.put("fangwubianhao", null);}else{pmap.put("fangwubianhao", fangwubianhao);}		if(fangwumingcheng==null||fangwumingcheng.equals("")){pmap.put("fangwumingcheng", null);}else{pmap.put("fangwumingcheng", fangwumingcheng);}		if(fangwuleibie==null||fangwuleibie.equals("")){pmap.put("fangwuleibie", null);}else{pmap.put("fangwuleibie", fangwuleibie);}		if(yuejiage==null||yuejiage.equals("")){pmap.put("yuejiage", null);}else{pmap.put("yuejiage", yuejiage);}		if(lianxidianhua==null||lianxidianhua.equals("")){pmap.put("lianxidianhua", null);}else{pmap.put("lianxidianhua", lianxidianhua);}		if(yonghuzhanghao==null||yonghuzhanghao.equals("")){pmap.put("yonghuzhanghao", null);}else{pmap.put("yonghuzhanghao", yonghuzhanghao);}		if(yonghuxingming==null||yonghuxingming.equals("")){pmap.put("yonghuxingming", null);}else{pmap.put("yonghuxingming", yonghuxingming);}		if(yonghudianhua==null||yonghudianhua.equals("")){pmap.put("yonghudianhua", null);}else{pmap.put("yonghudianhua", yonghudianhua);}		if(zulinyuefen==null||zulinyuefen.equals("")){pmap.put("zulinyuefen", null);}else{pmap.put("zulinyuefen", zulinyuefen);}		if(yingfujine==null||yingfujine.equals("")){pmap.put("yingfujine", null);}else{pmap.put("yingfujine", yingfujine);}		if(hetongzhaopian==null||hetongzhaopian.equals("")){pmap.put("hetongzhaopian", null);}else{pmap.put("hetongzhaopian", hetongzhaopian);}		if(hetongxiazai==null||hetongxiazai.equals("")){pmap.put("hetongxiazai", null);}else{pmap.put("hetongxiazai", hetongxiazai);}		if(shengxiaoshijian1==null||shengxiaoshijian1.equals("")){pmap.put("shengxiaoshijian1", null);}else{pmap.put("shengxiaoshijian1", shengxiaoshijian1);}		if(shengxiaoshijian2==null||shengxiaoshijian2.equals("")){pmap.put("shengxiaoshijian2", null);}else{pmap.put("shengxiaoshijian2", shengxiaoshijian2);}		if(daoqishijian1==null||daoqishijian1.equals("")){pmap.put("daoqishijian1", null);}else{pmap.put("daoqishijian1", daoqishijian1);}		if(daoqishijian2==null||daoqishijian2.equals("")){pmap.put("daoqishijian2", null);}else{pmap.put("daoqishijian2", daoqishijian2);}		
		int total=hetongxinxiService.getCount(pmap);
		pageBean.setTotal(total);
		List<Hetongxinxi> list=hetongxinxiService.getByPage(pmap);
		map.put("page", pageBean);
		map.put("list", list);
		session.setAttribute("p", 1);
		return "hetongxinxilist";
	}
	@RequestMapping("htxxListtp.do")
	public String htxxListtp(@RequestParam(value="page",required=false)String page,
			ModelMap map,HttpSession session,Hetongxinxi hetongxinxi, String fangwubianhao, String fangwumingcheng, String fangwuleibie, String yuejiage, String lianxidianhua, String yonghuzhanghao, String yonghuxingming, String yonghudianhua, String zulinyuefen, String yingfujine, String hetongzhaopian, String hetongxiazai, String shengxiaoshijian1,String shengxiaoshijian2, String daoqishijian1,String daoqishijian2){
		if(page==null||page.equals("")){
			page="1";
		}
		PageBean pageBean=new PageBean(Integer.parseInt(page), 5);
		Map<String, Object> pmap=new HashMap<String,Object>();
		pmap.put("pageno", pageBean.getStart());
		pmap.put("pageSize", 5);
		if(fangwubianhao==null||fangwubianhao.equals("")){pmap.put("fangwubianhao", null);}else{pmap.put("fangwubianhao", fangwubianhao);}		if(fangwumingcheng==null||fangwumingcheng.equals("")){pmap.put("fangwumingcheng", null);}else{pmap.put("fangwumingcheng", fangwumingcheng);}		if(fangwuleibie==null||fangwuleibie.equals("")){pmap.put("fangwuleibie", null);}else{pmap.put("fangwuleibie", fangwuleibie);}		if(yuejiage==null||yuejiage.equals("")){pmap.put("yuejiage", null);}else{pmap.put("yuejiage", yuejiage);}		if(lianxidianhua==null||lianxidianhua.equals("")){pmap.put("lianxidianhua", null);}else{pmap.put("lianxidianhua", lianxidianhua);}		if(yonghuzhanghao==null||yonghuzhanghao.equals("")){pmap.put("yonghuzhanghao", null);}else{pmap.put("yonghuzhanghao", yonghuzhanghao);}		if(yonghuxingming==null||yonghuxingming.equals("")){pmap.put("yonghuxingming", null);}else{pmap.put("yonghuxingming", yonghuxingming);}		if(yonghudianhua==null||yonghudianhua.equals("")){pmap.put("yonghudianhua", null);}else{pmap.put("yonghudianhua", yonghudianhua);}		if(zulinyuefen==null||zulinyuefen.equals("")){pmap.put("zulinyuefen", null);}else{pmap.put("zulinyuefen", zulinyuefen);}		if(yingfujine==null||yingfujine.equals("")){pmap.put("yingfujine", null);}else{pmap.put("yingfujine", yingfujine);}		if(hetongzhaopian==null||hetongzhaopian.equals("")){pmap.put("hetongzhaopian", null);}else{pmap.put("hetongzhaopian", hetongzhaopian);}		if(hetongxiazai==null||hetongxiazai.equals("")){pmap.put("hetongxiazai", null);}else{pmap.put("hetongxiazai", hetongxiazai);}		if(shengxiaoshijian1==null||shengxiaoshijian1.equals("")){pmap.put("shengxiaoshijian1", null);}else{pmap.put("shengxiaoshijian1", shengxiaoshijian1);}		if(shengxiaoshijian2==null||shengxiaoshijian2.equals("")){pmap.put("shengxiaoshijian2", null);}else{pmap.put("shengxiaoshijian2", shengxiaoshijian2);}		if(daoqishijian1==null||daoqishijian1.equals("")){pmap.put("daoqishijian1", null);}else{pmap.put("daoqishijian1", daoqishijian1);}		if(daoqishijian2==null||daoqishijian2.equals("")){pmap.put("daoqishijian2", null);}else{pmap.put("daoqishijian2", daoqishijian2);}		
		int total=hetongxinxiService.getCount(pmap);
		pageBean.setTotal(total);
		List<Hetongxinxi> list=hetongxinxiService.getByPage(pmap);
		map.put("page", pageBean);
		map.put("list", list);
		session.setAttribute("p", 1);
		return "hetongxinxilisttp";
	}
	
	@RequestMapping("deleteHetongxinxi.do")
	public String deleteHetongxinxi(int id,HttpServletRequest request,HttpSession session){
		hetongxinxiService.delete(id);
		session.setAttribute("backxx", "删除成功");session.setAttribute("backurl", request.getHeader("Referer"));return "redirect:postback.jsp";
	}
	
	
}

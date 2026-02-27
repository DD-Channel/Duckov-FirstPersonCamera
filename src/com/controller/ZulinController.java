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

import com.entity.Zulin;
import com.server.ZulinServer;
import com.util.PageBean;
import net.sf.json.JSONObject;
import com.util.db;
import java.sql.SQLException;
import java.sql.*;
@Controller
public class ZulinController {
	@Resource
	private ZulinServer zulinService;

	@RequestMapping("addZulin.do")
	public String addZulin(HttpServletRequest request,Zulin zulin,HttpSession session) throws SQLException{
		Timestamp time=new Timestamp(System.currentTimeMillis());
		
		zulin.setAddtime(time.toString().substring(0, 19));
		zulinService.add(zulin);
		db dbo = new db();
		
		//kuabiaogaizhi
		session.setAttribute("backxx", "添加成功");session.setAttribute("backurl", request.getHeader("Referer"));return "redirect:postback.jsp";
		
	}
	@RequestMapping("addZulinqt.do")
	public String addZulinqt(HttpServletRequest request,Zulin zulin,HttpSession session) throws SQLException{
		Timestamp time=new Timestamp(System.currentTimeMillis());
		
		zulin.setAddtime(time.toString().substring(0, 19));
		zulinService.add(zulin);
		db dbo = new db();
		
		//kuabiaogaizhi
		session.setAttribute("backxx", "添加成功");
		session.setAttribute("backurl", request.getHeader("Referer"));
		return "redirect:postback.jsp";
	}
 
//	处理编辑
	@RequestMapping("doUpdateZulin.do")
	public String doUpdateZulin(int id,ModelMap map,Zulin zulin){
		zulin=zulinService.getById(id);
		map.put("zulin", zulin);
		return "zulin_updt";
	}
	
	
	
	
//	后台详细
	@RequestMapping("zulinDetail.do")
	public String zulinDetail(int id,ModelMap map,Zulin zulin){
		zulin=zulinService.getById(id);
		map.put("zulin", zulin);
		return "zulin_detail";
	}
//	前台详细
	@RequestMapping("zlDetail.do")
	public String zlDetail(int id,ModelMap map,Zulin zulin){
		zulin=zulinService.getById(id);
		map.put("zulin", zulin);
		return "zulindetail";
	}
//	
	@RequestMapping("updateZulin.do")
	public String updateZulin(int id,ModelMap map,Zulin zulin,HttpServletRequest request,HttpSession session){
		zulinService.update(zulin);
		
		session.setAttribute("backxx", "修改成功");session.setAttribute("backurl", request.getHeader("Referer"));return "redirect:postback.jsp";
	}

//	分页查询
	@RequestMapping("zulinList.do")
	public String zulinList(@RequestParam(value="page",required=false)String page,
			ModelMap map,HttpSession session,Zulin zulin, String fangwubianhao, String fangwumingcheng, String fangwuleibie, String fangwudizhi, String fangwulouceng, String yuejiage, String lianxidianhua, String yonghuzhanghao, String yonghuxingming, String yonghudianhua, String zulinyuefen1,String zulinyuefen2, String yingfujine){
		if(page==null||page.equals("")){
			page="1";
		}
		PageBean pageBean=new PageBean(Integer.parseInt(page), 8);
		Map<String, Object> pmap=new HashMap<String,Object>();
		pmap.put("pageno", pageBean.getStart());
		pmap.put("pageSize", 8);
		
		
		if(fangwubianhao==null||fangwubianhao.equals("")){pmap.put("fangwubianhao", null);}else{pmap.put("fangwubianhao", fangwubianhao);}		if(fangwumingcheng==null||fangwumingcheng.equals("")){pmap.put("fangwumingcheng", null);}else{pmap.put("fangwumingcheng", fangwumingcheng);}		if(fangwuleibie==null||fangwuleibie.equals("")){pmap.put("fangwuleibie", null);}else{pmap.put("fangwuleibie", fangwuleibie);}		if(fangwudizhi==null||fangwudizhi.equals("")){pmap.put("fangwudizhi", null);}else{pmap.put("fangwudizhi", fangwudizhi);}		if(fangwulouceng==null||fangwulouceng.equals("")){pmap.put("fangwulouceng", null);}else{pmap.put("fangwulouceng", fangwulouceng);}		if(yuejiage==null||yuejiage.equals("")){pmap.put("yuejiage", null);}else{pmap.put("yuejiage", yuejiage);}		if(lianxidianhua==null||lianxidianhua.equals("")){pmap.put("lianxidianhua", null);}else{pmap.put("lianxidianhua", lianxidianhua);}		if(yonghuzhanghao==null||yonghuzhanghao.equals("")){pmap.put("yonghuzhanghao", null);}else{pmap.put("yonghuzhanghao", yonghuzhanghao);}		if(yonghuxingming==null||yonghuxingming.equals("")){pmap.put("yonghuxingming", null);}else{pmap.put("yonghuxingming", yonghuxingming);}		if(yonghudianhua==null||yonghudianhua.equals("")){pmap.put("yonghudianhua", null);}else{pmap.put("yonghudianhua", yonghudianhua);}		if(zulinyuefen1==null||zulinyuefen1.equals("")){pmap.put("zulinyuefen1", null);}else{pmap.put("zulinyuefen1", zulinyuefen1);}		if(zulinyuefen2==null||zulinyuefen2.equals("")){pmap.put("zulinyuefen2", null);}else{pmap.put("zulinyuefen2", zulinyuefen2);}		if(yingfujine==null||yingfujine.equals("")){pmap.put("yingfujine", null);}else{pmap.put("yingfujine", yingfujine);}		
		int total=zulinService.getCount(pmap);
		pageBean.setTotal(total);
		List<Zulin> list=zulinService.getByPage(pmap);
		map.put("page", pageBean);
		map.put("list", list);
		session.setAttribute("p", 1);
		return "zulin_list";
	}
	@RequestMapping("zulinList2.do")
	public String zulinList2(@RequestParam(value="page",required=false)String page,
			ModelMap map,HttpSession session,Zulin zulin, String fangwubianhao, String fangwumingcheng, String fangwuleibie, String fangwudizhi, String fangwulouceng, String yuejiage, String lianxidianhua, String yonghuzhanghao, String yonghuxingming, String yonghudianhua, String zulinyuefen1,String zulinyuefen2, String yingfujine,HttpServletRequest request){
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
		if(fangwubianhao==null||fangwubianhao.equals("")){pmap.put("fangwubianhao", null);}else{pmap.put("fangwubianhao", fangwubianhao);}		if(fangwumingcheng==null||fangwumingcheng.equals("")){pmap.put("fangwumingcheng", null);}else{pmap.put("fangwumingcheng", fangwumingcheng);}		if(fangwuleibie==null||fangwuleibie.equals("")){pmap.put("fangwuleibie", null);}else{pmap.put("fangwuleibie", fangwuleibie);}		if(fangwudizhi==null||fangwudizhi.equals("")){pmap.put("fangwudizhi", null);}else{pmap.put("fangwudizhi", fangwudizhi);}		if(fangwulouceng==null||fangwulouceng.equals("")){pmap.put("fangwulouceng", null);}else{pmap.put("fangwulouceng", fangwulouceng);}		if(yuejiage==null||yuejiage.equals("")){pmap.put("yuejiage", null);}else{pmap.put("yuejiage", yuejiage);}		if(lianxidianhua==null||lianxidianhua.equals("")){pmap.put("lianxidianhua", null);}else{pmap.put("lianxidianhua", lianxidianhua);}		if(yonghuxingming==null||yonghuxingming.equals("")){pmap.put("yonghuxingming", null);}else{pmap.put("yonghuxingming", yonghuxingming);}		if(yonghudianhua==null||yonghudianhua.equals("")){pmap.put("yonghudianhua", null);}else{pmap.put("yonghudianhua", yonghudianhua);}		if(zulinyuefen1==null||zulinyuefen1.equals("")){pmap.put("zulinyuefen1", null);}else{pmap.put("zulinyuefen1", zulinyuefen1);}		if(zulinyuefen2==null||zulinyuefen2.equals("")){pmap.put("zulinyuefen2", null);}else{pmap.put("zulinyuefen2", zulinyuefen2);}		if(yingfujine==null||yingfujine.equals("")){pmap.put("yingfujine", null);}else{pmap.put("yingfujine", yingfujine);}		
		
		int total=zulinService.getCount(pmap);
		pageBean.setTotal(total);
		List<Zulin> list=zulinService.getByPage(pmap);
		map.put("page", pageBean);
		map.put("list", list);
		session.setAttribute("p", 1);
		return "zulin_list2";
	}	
	
	@RequestMapping("zlList.do")
	public String zlList(@RequestParam(value="page",required=false)String page,
			ModelMap map,HttpSession session,Zulin zulin, String fangwubianhao, String fangwumingcheng, String fangwuleibie, String fangwudizhi, String fangwulouceng, String yuejiage, String lianxidianhua, String yonghuzhanghao, String yonghuxingming, String yonghudianhua, String zulinyuefen1,String zulinyuefen2, String yingfujine){
		if(page==null||page.equals("")){
			page="1";
		}
		PageBean pageBean=new PageBean(Integer.parseInt(page), 8);
		Map<String, Object> pmap=new HashMap<String,Object>();
		pmap.put("pageno", pageBean.getStart());
		pmap.put("pageSize", 8);
		if(fangwubianhao==null||fangwubianhao.equals("")){pmap.put("fangwubianhao", null);}else{pmap.put("fangwubianhao", fangwubianhao);}		if(fangwumingcheng==null||fangwumingcheng.equals("")){pmap.put("fangwumingcheng", null);}else{pmap.put("fangwumingcheng", fangwumingcheng);}		if(fangwuleibie==null||fangwuleibie.equals("")){pmap.put("fangwuleibie", null);}else{pmap.put("fangwuleibie", fangwuleibie);}		if(fangwudizhi==null||fangwudizhi.equals("")){pmap.put("fangwudizhi", null);}else{pmap.put("fangwudizhi", fangwudizhi);}		if(fangwulouceng==null||fangwulouceng.equals("")){pmap.put("fangwulouceng", null);}else{pmap.put("fangwulouceng", fangwulouceng);}		if(yuejiage==null||yuejiage.equals("")){pmap.put("yuejiage", null);}else{pmap.put("yuejiage", yuejiage);}		if(lianxidianhua==null||lianxidianhua.equals("")){pmap.put("lianxidianhua", null);}else{pmap.put("lianxidianhua", lianxidianhua);}		if(yonghuzhanghao==null||yonghuzhanghao.equals("")){pmap.put("yonghuzhanghao", null);}else{pmap.put("yonghuzhanghao", yonghuzhanghao);}		if(yonghuxingming==null||yonghuxingming.equals("")){pmap.put("yonghuxingming", null);}else{pmap.put("yonghuxingming", yonghuxingming);}		if(yonghudianhua==null||yonghudianhua.equals("")){pmap.put("yonghudianhua", null);}else{pmap.put("yonghudianhua", yonghudianhua);}		if(zulinyuefen1==null||zulinyuefen1.equals("")){pmap.put("zulinyuefen1", null);}else{pmap.put("zulinyuefen1", zulinyuefen1);}		if(zulinyuefen2==null||zulinyuefen2.equals("")){pmap.put("zulinyuefen2", null);}else{pmap.put("zulinyuefen2", zulinyuefen2);}		if(yingfujine==null||yingfujine.equals("")){pmap.put("yingfujine", null);}else{pmap.put("yingfujine", yingfujine);}		
		int total=zulinService.getCount(pmap);
		pageBean.setTotal(total);
		List<Zulin> list=zulinService.getByPage(pmap);
		map.put("page", pageBean);
		map.put("list", list);
		session.setAttribute("p", 1);
		return "zulinlist";
	}
	@RequestMapping("zlListtp.do")
	public String zlListtp(@RequestParam(value="page",required=false)String page,
			ModelMap map,HttpSession session,Zulin zulin, String fangwubianhao, String fangwumingcheng, String fangwuleibie, String fangwudizhi, String fangwulouceng, String yuejiage, String lianxidianhua, String yonghuzhanghao, String yonghuxingming, String yonghudianhua, String zulinyuefen1,String zulinyuefen2, String yingfujine){
		if(page==null||page.equals("")){
			page="1";
		}
		PageBean pageBean=new PageBean(Integer.parseInt(page), 8);
		Map<String, Object> pmap=new HashMap<String,Object>();
		pmap.put("pageno", pageBean.getStart());
		pmap.put("pageSize", 8);
		if(fangwubianhao==null||fangwubianhao.equals("")){pmap.put("fangwubianhao", null);}else{pmap.put("fangwubianhao", fangwubianhao);}		if(fangwumingcheng==null||fangwumingcheng.equals("")){pmap.put("fangwumingcheng", null);}else{pmap.put("fangwumingcheng", fangwumingcheng);}		if(fangwuleibie==null||fangwuleibie.equals("")){pmap.put("fangwuleibie", null);}else{pmap.put("fangwuleibie", fangwuleibie);}		if(fangwudizhi==null||fangwudizhi.equals("")){pmap.put("fangwudizhi", null);}else{pmap.put("fangwudizhi", fangwudizhi);}		if(fangwulouceng==null||fangwulouceng.equals("")){pmap.put("fangwulouceng", null);}else{pmap.put("fangwulouceng", fangwulouceng);}		if(yuejiage==null||yuejiage.equals("")){pmap.put("yuejiage", null);}else{pmap.put("yuejiage", yuejiage);}		if(lianxidianhua==null||lianxidianhua.equals("")){pmap.put("lianxidianhua", null);}else{pmap.put("lianxidianhua", lianxidianhua);}		if(yonghuzhanghao==null||yonghuzhanghao.equals("")){pmap.put("yonghuzhanghao", null);}else{pmap.put("yonghuzhanghao", yonghuzhanghao);}		if(yonghuxingming==null||yonghuxingming.equals("")){pmap.put("yonghuxingming", null);}else{pmap.put("yonghuxingming", yonghuxingming);}		if(yonghudianhua==null||yonghudianhua.equals("")){pmap.put("yonghudianhua", null);}else{pmap.put("yonghudianhua", yonghudianhua);}		if(zulinyuefen1==null||zulinyuefen1.equals("")){pmap.put("zulinyuefen1", null);}else{pmap.put("zulinyuefen1", zulinyuefen1);}		if(zulinyuefen2==null||zulinyuefen2.equals("")){pmap.put("zulinyuefen2", null);}else{pmap.put("zulinyuefen2", zulinyuefen2);}		if(yingfujine==null||yingfujine.equals("")){pmap.put("yingfujine", null);}else{pmap.put("yingfujine", yingfujine);}		
		int total=zulinService.getCount(pmap);
		pageBean.setTotal(total);
		List<Zulin> list=zulinService.getByPage(pmap);
		map.put("page", pageBean);
		map.put("list", list);
		session.setAttribute("p", 1);
		return "zulinlisttp";
	}
	
	@RequestMapping("deleteZulin.do")
	public String deleteZulin(int id,HttpServletRequest request,HttpSession session){
		zulinService.delete(id);
		session.setAttribute("backxx", "删除成功");session.setAttribute("backurl", request.getHeader("Referer"));return "redirect:postback.jsp";
	}
	
	
}

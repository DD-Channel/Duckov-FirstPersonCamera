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

import com.entity.Fangwuxinxi;
import com.server.FangwuxinxiServer;
import com.util.PageBean;
import net.sf.json.JSONObject;
import com.util.db;
import java.sql.SQLException;
import java.sql.*;
@Controller
public class FangwuxinxiController {
	@Resource
	private FangwuxinxiServer fangwuxinxiService;

	@RequestMapping("addFangwuxinxi.do")
	public String addFangwuxinxi(HttpServletRequest request,Fangwuxinxi fangwuxinxi,HttpSession session) throws SQLException{
		Timestamp time=new Timestamp(System.currentTimeMillis());
		
		fangwuxinxi.setAddtime(time.toString().substring(0, 19));
		fangwuxinxiService.add(fangwuxinxi);
		db dbo = new db();
		
		//kuabiaogaizhi
		session.setAttribute("backxx", "添加成功");session.setAttribute("backurl", request.getHeader("Referer"));return "redirect:postback.jsp";
		
	}
	@RequestMapping("addFangwuxinxiqt.do")
	public String addFangwuxinxiqt(HttpServletRequest request,Fangwuxinxi fangwuxinxi,HttpSession session) throws SQLException{
		Timestamp time=new Timestamp(System.currentTimeMillis());
		
		fangwuxinxi.setAddtime(time.toString().substring(0, 19));
		fangwuxinxiService.add(fangwuxinxi);
		db dbo = new db();
		
		//kuabiaogaizhi
		session.setAttribute("backxx", "添加成功");
		session.setAttribute("backurl", request.getHeader("Referer"));
		return "redirect:postback.jsp";
	}
 
//	处理编辑
	@RequestMapping("doUpdateFangwuxinxi.do")
	public String doUpdateFangwuxinxi(int id,ModelMap map,Fangwuxinxi fangwuxinxi){
		fangwuxinxi=fangwuxinxiService.getById(id);
		map.put("fangwuxinxi", fangwuxinxi);
		return "fangwuxinxi_updt";
	}
	
	
	
	
//	后台详细
	@RequestMapping("fangwuxinxiDetail.do")
	public String fangwuxinxiDetail(int id,ModelMap map,Fangwuxinxi fangwuxinxi){
		fangwuxinxi=fangwuxinxiService.getById(id);
		map.put("fangwuxinxi", fangwuxinxi);
		return "fangwuxinxi_detail";
	}
//	前台详细
	@RequestMapping("fwxxDetail.do")
	public String fwxxDetail(int id,ModelMap map,Fangwuxinxi fangwuxinxi){
		fangwuxinxi=fangwuxinxiService.getById(id);
		map.put("fangwuxinxi", fangwuxinxi);
		return "fangwuxinxidetail";
	}
//	
	@RequestMapping("updateFangwuxinxi.do")
	public String updateFangwuxinxi(int id,ModelMap map,Fangwuxinxi fangwuxinxi,HttpServletRequest request,HttpSession session){
		fangwuxinxiService.update(fangwuxinxi);
		
		session.setAttribute("backxx", "修改成功");session.setAttribute("backurl", request.getHeader("Referer"));return "redirect:postback.jsp";
	}

//	分页查询
	@RequestMapping("fangwuxinxiList.do")
	public String fangwuxinxiList(@RequestParam(value="page",required=false)String page,
			ModelMap map,HttpSession session,Fangwuxinxi fangwuxinxi, String fangwubianhao, String fangwumingcheng, String fangwuleibie, String fangwudizhi, String fangwumianji, String fangwulouceng, String fangwuzhaopian, String yuejiage1,String yuejiage2, String lianxidianhua, String beizhu){
		if(page==null||page.equals("")){
			page="1";
		}
		PageBean pageBean=new PageBean(Integer.parseInt(page), 5);
		Map<String, Object> pmap=new HashMap<String,Object>();
		pmap.put("pageno", pageBean.getStart());
		pmap.put("pageSize", 5);
		
		
		if(fangwubianhao==null||fangwubianhao.equals("")){pmap.put("fangwubianhao", null);}else{pmap.put("fangwubianhao", fangwubianhao);}		if(fangwumingcheng==null||fangwumingcheng.equals("")){pmap.put("fangwumingcheng", null);}else{pmap.put("fangwumingcheng", fangwumingcheng);}		if(fangwuleibie==null||fangwuleibie.equals("")){pmap.put("fangwuleibie", null);}else{pmap.put("fangwuleibie", fangwuleibie);}		if(fangwudizhi==null||fangwudizhi.equals("")){pmap.put("fangwudizhi", null);}else{pmap.put("fangwudizhi", fangwudizhi);}		if(fangwumianji==null||fangwumianji.equals("")){pmap.put("fangwumianji", null);}else{pmap.put("fangwumianji", fangwumianji);}		if(fangwulouceng==null||fangwulouceng.equals("")){pmap.put("fangwulouceng", null);}else{pmap.put("fangwulouceng", fangwulouceng);}		if(fangwuzhaopian==null||fangwuzhaopian.equals("")){pmap.put("fangwuzhaopian", null);}else{pmap.put("fangwuzhaopian", fangwuzhaopian);}		if(yuejiage1==null||yuejiage1.equals("")){pmap.put("yuejiage1", null);}else{pmap.put("yuejiage1", yuejiage1);}		if(yuejiage2==null||yuejiage2.equals("")){pmap.put("yuejiage2", null);}else{pmap.put("yuejiage2", yuejiage2);}		if(lianxidianhua==null||lianxidianhua.equals("")){pmap.put("lianxidianhua", null);}else{pmap.put("lianxidianhua", lianxidianhua);}		if(beizhu==null||beizhu.equals("")){pmap.put("beizhu", null);}else{pmap.put("beizhu", beizhu);}		
		int total=fangwuxinxiService.getCount(pmap);
		pageBean.setTotal(total);
		List<Fangwuxinxi> list=fangwuxinxiService.getByPage(pmap);
		map.put("page", pageBean);
		map.put("list", list);
		session.setAttribute("p", 1);
		return "fangwuxinxi_list";
	}
	
	
	@RequestMapping("fwxxList.do")
	public String fwxxList(@RequestParam(value="page",required=false)String page,
			ModelMap map,HttpSession session,Fangwuxinxi fangwuxinxi, String fangwubianhao, String fangwumingcheng, String fangwuleibie, String fangwudizhi, String fangwumianji, String fangwulouceng, String fangwuzhaopian, String yuejiage1,String yuejiage2, String lianxidianhua, String beizhu){
		if(page==null||page.equals("")){
			page="1";
		}
		PageBean pageBean=new PageBean(Integer.parseInt(page), 5);
		Map<String, Object> pmap=new HashMap<String,Object>();
		pmap.put("pageno", pageBean.getStart());
		pmap.put("pageSize", 5);
		if(fangwubianhao==null||fangwubianhao.equals("")){pmap.put("fangwubianhao", null);}else{pmap.put("fangwubianhao", fangwubianhao);}		if(fangwumingcheng==null||fangwumingcheng.equals("")){pmap.put("fangwumingcheng", null);}else{pmap.put("fangwumingcheng", fangwumingcheng);}		if(fangwuleibie==null||fangwuleibie.equals("")){pmap.put("fangwuleibie", null);}else{pmap.put("fangwuleibie", fangwuleibie);}		if(fangwudizhi==null||fangwudizhi.equals("")){pmap.put("fangwudizhi", null);}else{pmap.put("fangwudizhi", fangwudizhi);}		if(fangwumianji==null||fangwumianji.equals("")){pmap.put("fangwumianji", null);}else{pmap.put("fangwumianji", fangwumianji);}		if(fangwulouceng==null||fangwulouceng.equals("")){pmap.put("fangwulouceng", null);}else{pmap.put("fangwulouceng", fangwulouceng);}		if(fangwuzhaopian==null||fangwuzhaopian.equals("")){pmap.put("fangwuzhaopian", null);}else{pmap.put("fangwuzhaopian", fangwuzhaopian);}		if(yuejiage1==null||yuejiage1.equals("")){pmap.put("yuejiage1", null);}else{pmap.put("yuejiage1", yuejiage1);}		if(yuejiage2==null||yuejiage2.equals("")){pmap.put("yuejiage2", null);}else{pmap.put("yuejiage2", yuejiage2);}		if(lianxidianhua==null||lianxidianhua.equals("")){pmap.put("lianxidianhua", null);}else{pmap.put("lianxidianhua", lianxidianhua);}		if(beizhu==null||beizhu.equals("")){pmap.put("beizhu", null);}else{pmap.put("beizhu", beizhu);}		
		int total=fangwuxinxiService.getCount(pmap);
		pageBean.setTotal(total);
		List<Fangwuxinxi> list=fangwuxinxiService.getByPage(pmap);
		map.put("page", pageBean);
		map.put("list", list);
		session.setAttribute("p", 1);
		return "fangwuxinxilist";
	}
	@RequestMapping("fwxxListtp.do")
	public String fwxxListtp(@RequestParam(value="page",required=false)String page,
			ModelMap map,HttpSession session,Fangwuxinxi fangwuxinxi, String fangwubianhao, String fangwumingcheng, String fangwuleibie, String fangwudizhi, String fangwumianji, String fangwulouceng, String fangwuzhaopian, String yuejiage1,String yuejiage2, String lianxidianhua, String beizhu){
		if(page==null||page.equals("")){
			page="1";
		}
		PageBean pageBean=new PageBean(Integer.parseInt(page), 5);
		Map<String, Object> pmap=new HashMap<String,Object>();
		pmap.put("pageno", pageBean.getStart());
		pmap.put("pageSize", 5);
		if(fangwubianhao==null||fangwubianhao.equals("")){pmap.put("fangwubianhao", null);}else{pmap.put("fangwubianhao", fangwubianhao);}		if(fangwumingcheng==null||fangwumingcheng.equals("")){pmap.put("fangwumingcheng", null);}else{pmap.put("fangwumingcheng", fangwumingcheng);}		if(fangwuleibie==null||fangwuleibie.equals("")){pmap.put("fangwuleibie", null);}else{pmap.put("fangwuleibie", fangwuleibie);}		if(fangwudizhi==null||fangwudizhi.equals("")){pmap.put("fangwudizhi", null);}else{pmap.put("fangwudizhi", fangwudizhi);}		if(fangwumianji==null||fangwumianji.equals("")){pmap.put("fangwumianji", null);}else{pmap.put("fangwumianji", fangwumianji);}		if(fangwulouceng==null||fangwulouceng.equals("")){pmap.put("fangwulouceng", null);}else{pmap.put("fangwulouceng", fangwulouceng);}		if(fangwuzhaopian==null||fangwuzhaopian.equals("")){pmap.put("fangwuzhaopian", null);}else{pmap.put("fangwuzhaopian", fangwuzhaopian);}		if(yuejiage1==null||yuejiage1.equals("")){pmap.put("yuejiage1", null);}else{pmap.put("yuejiage1", yuejiage1);}		if(yuejiage2==null||yuejiage2.equals("")){pmap.put("yuejiage2", null);}else{pmap.put("yuejiage2", yuejiage2);}		if(lianxidianhua==null||lianxidianhua.equals("")){pmap.put("lianxidianhua", null);}else{pmap.put("lianxidianhua", lianxidianhua);}		if(beizhu==null||beizhu.equals("")){pmap.put("beizhu", null);}else{pmap.put("beizhu", beizhu);}		
		int total=fangwuxinxiService.getCount(pmap);
		pageBean.setTotal(total);
		List<Fangwuxinxi> list=fangwuxinxiService.getByPage(pmap);
		map.put("page", pageBean);
		map.put("list", list);
		session.setAttribute("p", 1);
		return "fangwuxinxilisttp";
	}
	
	@RequestMapping("deleteFangwuxinxi.do")
	public String deleteFangwuxinxi(int id,HttpServletRequest request,HttpSession session){
		fangwuxinxiService.delete(id);
		session.setAttribute("backxx", "删除成功");session.setAttribute("backurl", request.getHeader("Referer"));return "redirect:postback.jsp";
	}
	
	@RequestMapping("quchongFangwuxinxi.do")
	public void quchongFangwuxinxi(Fangwuxinxi fangwuxinxi,HttpServletResponse response){
		   Map<String,Object> map=new HashMap<String,Object>();
		   map.put("fangwubianhao", fangwuxinxi.getFangwubianhao());
		   System.out.println("fangwubianhao==="+fangwuxinxi.getFangwubianhao());
		   System.out.println("fangwubianhao222==="+fangwuxinxiService.quchongFangwuxinxi(map));
		   JSONObject obj=new JSONObject();
		   if(fangwuxinxiService.quchongFangwuxinxi(map)!=null){
				 obj.put("info", "ng");
			   }else{
				   obj.put("info", "房屋编号可以用！");
				  
			   }
		   response.setContentType("text/html;charset=utf-8");
		   PrintWriter out=null;
		   try {
			out=response.getWriter();
			out.print(obj);
			out.flush();
		} catch (IOException e) {
			e.printStackTrace();
		}finally{
			out.close();
		}
	}
}

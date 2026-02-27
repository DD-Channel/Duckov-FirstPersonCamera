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

import com.entity.Yonghuxinxi;
import com.server.YonghuxinxiServer;
import com.util.PageBean;
import net.sf.json.JSONObject;
import com.util.db;
import java.sql.SQLException;
import java.sql.*;
@Controller
public class YonghuxinxiController {
	@Resource
	private YonghuxinxiServer yonghuxinxiService;

	@RequestMapping("addYonghuxinxi.do")
	public String addYonghuxinxi(HttpServletRequest request,Yonghuxinxi yonghuxinxi,HttpSession session) throws SQLException{
		Timestamp time=new Timestamp(System.currentTimeMillis());
		
		yonghuxinxi.setAddtime(time.toString().substring(0, 19));
		yonghuxinxiService.add(yonghuxinxi);
		db dbo = new db();
		
		//kuabiaogaizhi
		session.setAttribute("backxx", "添加成功");session.setAttribute("backurl", request.getHeader("Referer"));return "redirect:postback.jsp";
		
	}
	@RequestMapping("addYonghuxinxiqt.do")
	public String addYonghuxinxiqt(HttpServletRequest request,Yonghuxinxi yonghuxinxi,HttpSession session) throws SQLException{
		Timestamp time=new Timestamp(System.currentTimeMillis());
		
		yonghuxinxi.setAddtime(time.toString().substring(0, 19));
		yonghuxinxiService.add(yonghuxinxi);
		db dbo = new db();
		
		//kuabiaogaizhi
		session.setAttribute("backxx", "添加成功");
		session.setAttribute("backurl", request.getHeader("Referer"));
		return "redirect:postback.jsp";
	}
 
//	处理编辑
	@RequestMapping("doUpdateYonghuxinxi.do")
	public String doUpdateYonghuxinxi(int id,ModelMap map,Yonghuxinxi yonghuxinxi){
		yonghuxinxi=yonghuxinxiService.getById(id);
		map.put("yonghuxinxi", yonghuxinxi);
		return "yonghuxinxi_updt";
	}
	
	@RequestMapping("doUpdateYonghuxinxi2.do")
	public String doUpdateYonghuxinxi2(ModelMap map,Yonghuxinxi yonghuxinxi,HttpServletRequest request){
		yonghuxinxi=yonghuxinxiService.getById(Integer.parseInt((String)request.getSession().getAttribute("uid")));
		map.put("yonghuxinxi", yonghuxinxi);
		return "yonghuxinxi_updt2";
	}
	
@RequestMapping("updateYonghuxinxi2.do")
	public String updateYonghuxinxi2(int id,ModelMap map,Yonghuxinxi yonghuxinxi){
		yonghuxinxiService.update(yonghuxinxi);
		return "redirect:doUpdateYonghuxinxi2.do";
	}
	
	
//	后台详细
	@RequestMapping("yonghuxinxiDetail.do")
	public String yonghuxinxiDetail(int id,ModelMap map,Yonghuxinxi yonghuxinxi){
		yonghuxinxi=yonghuxinxiService.getById(id);
		map.put("yonghuxinxi", yonghuxinxi);
		return "yonghuxinxi_detail";
	}
//	前台详细
	@RequestMapping("yhxxDetail.do")
	public String yhxxDetail(int id,ModelMap map,Yonghuxinxi yonghuxinxi){
		yonghuxinxi=yonghuxinxiService.getById(id);
		map.put("yonghuxinxi", yonghuxinxi);
		return "yonghuxinxidetail";
	}
//	
	@RequestMapping("updateYonghuxinxi.do")
	public String updateYonghuxinxi(int id,ModelMap map,Yonghuxinxi yonghuxinxi,HttpServletRequest request,HttpSession session){
		yonghuxinxiService.update(yonghuxinxi);
		
		session.setAttribute("backxx", "修改成功");session.setAttribute("backurl", request.getHeader("Referer"));return "redirect:postback.jsp";
	}

//	分页查询
	@RequestMapping("yonghuxinxiList.do")
	public String yonghuxinxiList(@RequestParam(value="page",required=false)String page,
			ModelMap map,HttpSession session,Yonghuxinxi yonghuxinxi, String yonghuzhanghao, String mima, String yonghuxingming, String xingbie, String yonghuzhaopian, String yonghudianhua){
		if(page==null||page.equals("")){
			page="1";
		}
		PageBean pageBean=new PageBean(Integer.parseInt(page), 5);
		Map<String, Object> pmap=new HashMap<String,Object>();
		pmap.put("pageno", pageBean.getStart());
		pmap.put("pageSize", 5);
		
		
		if(yonghuzhanghao==null||yonghuzhanghao.equals("")){pmap.put("yonghuzhanghao", null);}else{pmap.put("yonghuzhanghao", yonghuzhanghao);}		if(mima==null||mima.equals("")){pmap.put("mima", null);}else{pmap.put("mima", mima);}		if(yonghuxingming==null||yonghuxingming.equals("")){pmap.put("yonghuxingming", null);}else{pmap.put("yonghuxingming", yonghuxingming);}		if(xingbie==null||xingbie.equals("")){pmap.put("xingbie", null);}else{pmap.put("xingbie", xingbie);}		if(yonghuzhaopian==null||yonghuzhaopian.equals("")){pmap.put("yonghuzhaopian", null);}else{pmap.put("yonghuzhaopian", yonghuzhaopian);}		if(yonghudianhua==null||yonghudianhua.equals("")){pmap.put("yonghudianhua", null);}else{pmap.put("yonghudianhua", yonghudianhua);}		
		int total=yonghuxinxiService.getCount(pmap);
		pageBean.setTotal(total);
		List<Yonghuxinxi> list=yonghuxinxiService.getByPage(pmap);
		map.put("page", pageBean);
		map.put("list", list);
		session.setAttribute("p", 1);
		return "yonghuxinxi_list";
	}
	
	
	@RequestMapping("yhxxList.do")
	public String yhxxList(@RequestParam(value="page",required=false)String page,
			ModelMap map,HttpSession session,Yonghuxinxi yonghuxinxi, String yonghuzhanghao, String mima, String yonghuxingming, String xingbie, String yonghuzhaopian, String yonghudianhua){
		if(page==null||page.equals("")){
			page="1";
		}
		PageBean pageBean=new PageBean(Integer.parseInt(page), 5);
		Map<String, Object> pmap=new HashMap<String,Object>();
		pmap.put("pageno", pageBean.getStart());
		pmap.put("pageSize", 5);
		if(yonghuzhanghao==null||yonghuzhanghao.equals("")){pmap.put("yonghuzhanghao", null);}else{pmap.put("yonghuzhanghao", yonghuzhanghao);}		if(mima==null||mima.equals("")){pmap.put("mima", null);}else{pmap.put("mima", mima);}		if(yonghuxingming==null||yonghuxingming.equals("")){pmap.put("yonghuxingming", null);}else{pmap.put("yonghuxingming", yonghuxingming);}		if(xingbie==null||xingbie.equals("")){pmap.put("xingbie", null);}else{pmap.put("xingbie", xingbie);}		if(yonghuzhaopian==null||yonghuzhaopian.equals("")){pmap.put("yonghuzhaopian", null);}else{pmap.put("yonghuzhaopian", yonghuzhaopian);}		if(yonghudianhua==null||yonghudianhua.equals("")){pmap.put("yonghudianhua", null);}else{pmap.put("yonghudianhua", yonghudianhua);}		
		int total=yonghuxinxiService.getCount(pmap);
		pageBean.setTotal(total);
		List<Yonghuxinxi> list=yonghuxinxiService.getByPage(pmap);
		map.put("page", pageBean);
		map.put("list", list);
		session.setAttribute("p", 1);
		return "yonghuxinxilist";
	}
	@RequestMapping("yhxxListtp.do")
	public String yhxxListtp(@RequestParam(value="page",required=false)String page,
			ModelMap map,HttpSession session,Yonghuxinxi yonghuxinxi, String yonghuzhanghao, String mima, String yonghuxingming, String xingbie, String yonghuzhaopian, String yonghudianhua){
		if(page==null||page.equals("")){
			page="1";
		}
		PageBean pageBean=new PageBean(Integer.parseInt(page), 5);
		Map<String, Object> pmap=new HashMap<String,Object>();
		pmap.put("pageno", pageBean.getStart());
		pmap.put("pageSize", 5);
		if(yonghuzhanghao==null||yonghuzhanghao.equals("")){pmap.put("yonghuzhanghao", null);}else{pmap.put("yonghuzhanghao", yonghuzhanghao);}		if(mima==null||mima.equals("")){pmap.put("mima", null);}else{pmap.put("mima", mima);}		if(yonghuxingming==null||yonghuxingming.equals("")){pmap.put("yonghuxingming", null);}else{pmap.put("yonghuxingming", yonghuxingming);}		if(xingbie==null||xingbie.equals("")){pmap.put("xingbie", null);}else{pmap.put("xingbie", xingbie);}		if(yonghuzhaopian==null||yonghuzhaopian.equals("")){pmap.put("yonghuzhaopian", null);}else{pmap.put("yonghuzhaopian", yonghuzhaopian);}		if(yonghudianhua==null||yonghudianhua.equals("")){pmap.put("yonghudianhua", null);}else{pmap.put("yonghudianhua", yonghudianhua);}		
		int total=yonghuxinxiService.getCount(pmap);
		pageBean.setTotal(total);
		List<Yonghuxinxi> list=yonghuxinxiService.getByPage(pmap);
		map.put("page", pageBean);
		map.put("list", list);
		session.setAttribute("p", 1);
		return "yonghuxinxilisttp";
	}
	
	@RequestMapping("deleteYonghuxinxi.do")
	public String deleteYonghuxinxi(int id,HttpServletRequest request,HttpSession session){
		yonghuxinxiService.delete(id);
		session.setAttribute("backxx", "删除成功");session.setAttribute("backurl", request.getHeader("Referer"));return "redirect:postback.jsp";
	}
	
	@RequestMapping("quchongYonghuxinxi.do")
	public void quchongYonghuxinxi(Yonghuxinxi yonghuxinxi,HttpServletResponse response){
		   Map<String,Object> map=new HashMap<String,Object>();
		   map.put("yonghuzhanghao", yonghuxinxi.getYonghuzhanghao());
		   System.out.println("yonghuzhanghao==="+yonghuxinxi.getYonghuzhanghao());
		   System.out.println("yonghuzhanghao222==="+yonghuxinxiService.quchongYonghuxinxi(map));
		   JSONObject obj=new JSONObject();
		   if(yonghuxinxiService.quchongYonghuxinxi(map)!=null){
				 obj.put("info", "ng");
			   }else{
				   obj.put("info", "用户账号可以用！");
				  
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

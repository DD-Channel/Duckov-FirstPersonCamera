package com.entity;

public class Yonghuxinxi {
    private Integer id;
	private String yonghuzhanghao;	private String mima;	private String yonghuxingming;	private String xingbie;	private String yonghuzhaopian;	private String yonghudianhua;	
    private String addtime;

    

    public Integer getId() {
        return id;
    }
    public void setId(Integer id) {
        this.id = id;
    }
	
	public String getYonghuzhanghao() {
        return yonghuzhanghao;
    }
    public void setYonghuzhanghao(String yonghuzhanghao) {
        this.yonghuzhanghao = yonghuzhanghao == null ? null : yonghuzhanghao.trim();
    }	public String getMima() {
        return mima;
    }
    public void setMima(String mima) {
        this.mima = mima == null ? null : mima.trim();
    }	public String getYonghuxingming() {
        return yonghuxingming;
    }
    public void setYonghuxingming(String yonghuxingming) {
        this.yonghuxingming = yonghuxingming == null ? null : yonghuxingming.trim();
    }	public String getXingbie() {
        return xingbie;
    }
    public void setXingbie(String xingbie) {
        this.xingbie = xingbie == null ? null : xingbie.trim();
    }	public String getYonghuzhaopian() {
        return yonghuzhaopian;
    }
    public void setYonghuzhaopian(String yonghuzhaopian) {
        this.yonghuzhaopian = yonghuzhaopian == null ? null : yonghuzhaopian.trim();
    }	public String getYonghudianhua() {
        return yonghudianhua;
    }
    public void setYonghudianhua(String yonghudianhua) {
        this.yonghudianhua = yonghudianhua == null ? null : yonghudianhua.trim();
    }	
	
	
    public String getAddtime() {
        return addtime;
    }
    public void setAddtime(String addtime) {
        this.addtime = addtime == null ? null : addtime.trim();
    }
}
//   设置字段信息

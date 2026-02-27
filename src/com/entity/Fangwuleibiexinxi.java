package com.entity;

public class Fangwuleibiexinxi {
    private Integer id;
	private String fangwuleibie;	
    private String addtime;

    

    public Integer getId() {
        return id;
    }
    public void setId(Integer id) {
        this.id = id;
    }
	
	public String getFangwuleibie() {
        return fangwuleibie;
    }
    public void setFangwuleibie(String fangwuleibie) {
        this.fangwuleibie = fangwuleibie == null ? null : fangwuleibie.trim();
    }	
	
	
    public String getAddtime() {
        return addtime;
    }
    public void setAddtime(String addtime) {
        this.addtime = addtime == null ? null : addtime.trim();
    }
}
//   设置字段信息

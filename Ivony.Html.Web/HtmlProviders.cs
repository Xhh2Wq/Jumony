﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ivony.Html.Parser;
using System.Web.Hosting;
using System.Web;
using System.IO;

namespace Ivony.Html.Web
{


  public static class HtmlProviders
  {

    static HtmlProviders()
    {
      ParserProviders = new SynchronizedCollection<IHtmlParserProvider>( _parserProvidersSync );
      Loaders = new SynchronizedCollection<IHtmlLoader>( _loadersSync );
      Mappers = new SynchronizedCollection<IRequestMapper>( _mapperSync );


      Loaders.Add( new StaticFileLoader() );
      Loaders.Add( new AspxFileLoader() );

      Mappers.Add( new DefaultRequestMapper() );
    }


    private static readonly object _parserProvidersSync = new object();

    public static ICollection<IHtmlParserProvider> ParserProviders
    {
      get;
      private set;
    }


    private static readonly object _loadersSync = new object();

    public static ICollection<IHtmlLoader> Loaders
    {
      get;
      private set;
    }


    private static readonly object _mapperSync = new object();

    public static ICollection<IRequestMapper> Mappers
    {
      get;
      private set;
    }


    public static MapInfo MapRequest( HttpRequest request )
    {

      lock ( _mapperSync )
      {
        foreach ( var mapper in Mappers )
        {
          var result = mapper.MapRequest( request );
          if ( result != null )
          {
            result.Mapper = mapper;
            return result;
          }
        }
      }


      return null;
    }



    /// <summary>
    /// 获取用于分析 HTML 文档的 Parser
    /// </summary>
    /// <param name="virtualPath">请求的虚拟路径</param>
    /// <param name="htmlContent">HTML 文档内容</param>
    /// <returns>分析后的 HTML 文档</returns>
    public static IHtmlParser GetParser( HttpContextBase context, string virtualPath, string htmlContent )
    {

      if ( context == null )
        throw new ArgumentNullException( "context" );

      lock ( _parserProvidersSync )
      {
        foreach ( var provider in ParserProviders )
        {
          var parser = provider.GetParser( context, virtualPath, htmlContent );

          if ( parser != null )
            return parser;
        }
      }

      return new JumonyHtmlParser();
    }


    /// <summary>
    /// 加载 HTML 文档内容
    /// </summary>
    /// <param name="virtualPath">请求的虚拟路径</param>
    /// <returns>HTML 文档内容</returns>
    public static string LoadContent( HttpContextBase context, string virtualPath )
    {

      if ( context == null )
        throw new ArgumentNullException( "context" );


      lock ( _loadersSync )
      {
        foreach ( var provider in Loaders )
        {
          var content = provider.Load( context, virtualPath );

          if ( content != null )
            return content;
        }
      }


      return null;
    }

    /// <summary>
    /// 加载 HTML 文档
    /// </summary>
    /// <param name="virtualPath">请求的虚拟路径</param>
    /// <returns>HTML 文档对象</returns>
    public static IHtmlDocument LoadDocument( HttpContextBase context, string virtualPath )
    {

      if ( context == null )
        throw new ArgumentNullException( "context" );


      var content = LoadContent( context, virtualPath );
      if ( content == null )
        return null;

      var parser = GetParser( context, virtualPath, content );

      return parser.Parse( content );
    }
  }

}
